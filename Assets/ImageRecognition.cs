using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using TMPro;

// Structure pour stocker les informations de l'image reconnue
// plus simple à parcourir qu'un dictionnaire
public class KeyValuePair
{
    public string Key { get; set; }
    public int Value { get; set; }
}

// Ce script requiert un composant ARTrackedImageManager dans l'inspecteur unity
[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageRecognition : MonoBehaviour
{
    private ARTrackedImageManager aRTrackedImageManager;
    //Elements lié à l'UI (Pas totalement implémenté pour le moment)
    [SerializeField] TMP_Dropdown listeDeroulante;
    [SerializeField] private GameObject popup;
    [SerializeField] private Information information;

    // Variables pour stocker les objets instantiés
    private GameObject selectedPrefab;
    [SerializeField]
    private GameObject[] placeablePrefabs;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private List<KeyValuePair> prefabValue = new List<KeyValuePair>();

    // Manager pour détecter les images référencées
    private ARTrackedImageManager trackedImageManager;


    // Fonction appelée lorsque l'utilisateur sélectionne une image dans la liste déroulante
    // ne fonctionne pas pour le moment
    public void OnChange()
    {

        foreach (KeyValuePair objet in prefabValue)
        {
            if (objet.Value == listeDeroulante.value)
            {
                //Voir pour remplacer ça par une fenêtre peut être qui explique briévement le truc
                //popupText.text = "Vous avez sélectionné : " + objet.Key;
                OuvrirInformation(objet.Key);
                break;
            }
        }
    }

    //Cette fonction permet d'afficher un popup d'explication sur la pathologie sélectionnée
    //a termes, le but serait d'avoir quelque chose de plus interactif à l'image de la maquette.
    private void OuvrirInformation(string pathologie)
    {
        information.gameObject.SetActive(true);
        information.fermer.onClick.AddListener(FermerClicked);
        if (pathologie == "AMYGDALE")
        {
            information.message.text = "Ton doudou a mal à la gorge et/ou aux oreilles depuis quelques temps ?\nIl a des otites et angines à répétitions ?\nIl a une sensation de gêne constante dans la gorge ? \n\nSoigne les amygdales de doudou !";
        }
        else
        {
            information.message.text = "Si ton doudou tombe sur son poignet il peut se casser. C'est ce qu'on appelle une fracture. Dans son cas, c'est le radius qui est fracturé.\nLe radius est un des os du poignet. Il faut réparer cet os, sinon tu ne pourras plus jouer avec ton doudou.\n\nSoigne le poignet de doudou !";
        }
        
    }

    private void FermerClicked()
    {
        information.gameObject.SetActive(false);
        Debug.Log("fermer");
    }

    // Fonction pour afficher un message lorsque l'utilisateur clique sur l'image reconnue
    // remplacer pour le moment par la fonction on change qui prend effet lorsqu'on
    // choisit une pathologie.
    public void ShowPopup()
    {
        popup.SetActive(true);
        //popupText.text = "Touché !";
    }

    private void Awake()
    {
        information.gameObject.SetActive(false);
        // Récupération du ARTrackedImageManager
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        // i sert à attribuer la valeur d'un prefab à la valeur équivalente du tmp_dropdown
        int i = 1;
        // Instantiation des objets préfabriqués pour chaque image référencée
        foreach (GameObject prefab in placeablePrefabs)
        {
            //On instantie les objets avec des positions et rotation par défaut
            GameObject newPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            newPrefab.name = prefab.name;
            //Ajout du prefab créé au dictionnaire 
            spawnedPrefabs.Add(prefab.name, newPrefab);
            //Désactivation des prefab créé pour qu'ils n'apparaissent pas sur la scène avant
            //de scanner un QR code
            newPrefab.SetActive(false);
            //On associe enfin dans une liste des paires de noms de prefab et leur valeurs
            //(en rapport au TMP_Dropdown)
            prefabValue.Add(new KeyValuePair() { Key = prefab.name, Value = i });
            i++;
        }
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += ImageChanged;
    }
    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= ImageChanged;
    }

    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string name = trackedImage.referenceImage.name;
        //Traitement des cas ou on souhaite afficher spécifiquement la pathologie séléctionnée dans la liste
        if ((name == "OS_BAS" || name == "OS_HAUT") && listeDeroulante.value == 3) { return; }
        if ((listeDeroulante.value == 1 || listeDeroulante.value == 2) && name == "AMYGDALE") { return; }
        if (name == "AMYGDALE" && listeDeroulante.value != 0)
        {
            spawnedPrefabs["OS_HAUT"].SetActive(false);
            spawnedPrefabs["OS_BAS"].SetActive(false);
        }
        if ((name == "OS_HAUT" || name == "OS_BAS") && listeDeroulante.value != 0)
        {
            spawnedPrefabs["AMYGDALE"].SetActive(false);
        }

        Debug.Log(name);
        Vector3 position = trackedImage.transform.position;
        Quaternion rotation = trackedImage.transform.rotation;

        GameObject prefab = spawnedPrefabs[name];

        // Tentative d'implémentation de l'effet présent sur les maquettes
        // à savoir la fenêtre présentant la pathologie en touchant un modèle
        // 3D etc...
        if (prefab == selectedPrefab)
        {
            // Ajouter un composant EventTrigger à l'objet préfabriqué
            EventTrigger trigger = prefab.AddComponent<EventTrigger>();
            // Créer une nouvelle entrée d'événement
            EventTrigger.Entry entry = new EventTrigger.Entry();
            // Définir l'événement à détecter comme un contact tactile (PointerClick)
            entry.eventID = EventTriggerType.PointerClick;
            // Ajouter une fonction de rappel pour appeler ShowPopup() lorsque l'événement se produit
            entry.callback.AddListener((eventData) => { ShowPopup(); });
            // Ajouter l'entrée d'événement au composant EventTrigger
            trigger.triggers.Add(entry);
        }
        prefab.transform.position = position;
        prefab.transform.rotation = rotation;
        prefab.SetActive(true);
    }

    // Fonction appelée lorsque l'état des images référencées change
    private void ImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Pour chaque image ajoutée, mettre à jour la position et la rotation de l'objet correspondant
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateImage(trackedImage);
        }
        // Pour chaque image mise à jour, mettre à jour la position et la rotation de l'objet correspondant
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateImage(trackedImage);
        }
        // Pour chaque image supprimée, désactiver l'objet correspondant
        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            spawnedPrefabs[trackedImage.name].SetActive(false);
        }
    }
}
