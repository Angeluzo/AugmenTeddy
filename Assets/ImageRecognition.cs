using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using TMPro;

// Structure pour stocker les informations de l'image reconnue
// plus simple � parcourir qu'un dictionnaire
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
    //Elements li� � l'UI (Pas totalement impl�ment� pour le moment)
    [SerializeField] TMP_Dropdown listeDeroulante;
    [SerializeField] private GameObject popup;
    [SerializeField] private Information information;

    // Variables pour stocker les objets instanti�s
    private GameObject selectedPrefab;
    [SerializeField]
    private GameObject[] placeablePrefabs;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private List<KeyValuePair> prefabValue = new List<KeyValuePair>();

    // Manager pour d�tecter les images r�f�renc�es
    private ARTrackedImageManager trackedImageManager;


    // Fonction appel�e lorsque l'utilisateur s�lectionne une image dans la liste d�roulante
    // ne fonctionne pas pour le moment
    public void OnChange()
    {

        foreach (KeyValuePair objet in prefabValue)
        {
            if (objet.Value == listeDeroulante.value)
            {
                //Voir pour remplacer �a par une fen�tre peut �tre qui explique bri�vement le truc
                //popupText.text = "Vous avez s�lectionn� : " + objet.Key;
                OuvrirInformation(objet.Key);
                break;
            }
        }
    }

    //Cette fonction permet d'afficher un popup d'explication sur la pathologie s�lectionn�e
    //a termes, le but serait d'avoir quelque chose de plus interactif � l'image de la maquette.
    private void OuvrirInformation(string pathologie)
    {
        information.gameObject.SetActive(true);
        information.fermer.onClick.AddListener(FermerClicked);
        if (pathologie == "AMYGDALE")
        {
            information.message.text = "Ton doudou a mal � la gorge et/ou aux oreilles depuis quelques temps ?\nIl a des otites et angines � r�p�titions ?\nIl a une sensation de g�ne constante dans la gorge ? \n\nSoigne les amygdales de doudou !";
        }
        else
        {
            information.message.text = "Si ton doudou tombe sur son poignet il peut se casser. C'est ce qu'on appelle une fracture. Dans son cas, c'est le radius qui est fractur�.\nLe radius est un des os du poignet. Il faut r�parer cet os, sinon tu ne pourras plus jouer avec ton doudou.\n\nSoigne le poignet de doudou !";
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
        //popupText.text = "Touch� !";
    }

    private void Awake()
    {
        information.gameObject.SetActive(false);
        // R�cup�ration du ARTrackedImageManager
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        // i sert � attribuer la valeur d'un prefab � la valeur �quivalente du tmp_dropdown
        int i = 1;
        // Instantiation des objets pr�fabriqu�s pour chaque image r�f�renc�e
        foreach (GameObject prefab in placeablePrefabs)
        {
            //On instantie les objets avec des positions et rotation par d�faut
            GameObject newPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            newPrefab.name = prefab.name;
            //Ajout du prefab cr�� au dictionnaire 
            spawnedPrefabs.Add(prefab.name, newPrefab);
            //D�sactivation des prefab cr�� pour qu'ils n'apparaissent pas sur la sc�ne avant
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
        //Traitement des cas ou on souhaite afficher sp�cifiquement la pathologie s�l�ctionn�e dans la liste
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

        // Tentative d'impl�mentation de l'effet pr�sent sur les maquettes
        // � savoir la fen�tre pr�sentant la pathologie en touchant un mod�le
        // 3D etc...
        if (prefab == selectedPrefab)
        {
            // Ajouter un composant EventTrigger � l'objet pr�fabriqu�
            EventTrigger trigger = prefab.AddComponent<EventTrigger>();
            // Cr�er une nouvelle entr�e d'�v�nement
            EventTrigger.Entry entry = new EventTrigger.Entry();
            // D�finir l'�v�nement � d�tecter comme un contact tactile (PointerClick)
            entry.eventID = EventTriggerType.PointerClick;
            // Ajouter une fonction de rappel pour appeler ShowPopup() lorsque l'�v�nement se produit
            entry.callback.AddListener((eventData) => { ShowPopup(); });
            // Ajouter l'entr�e d'�v�nement au composant EventTrigger
            trigger.triggers.Add(entry);
        }
        prefab.transform.position = position;
        prefab.transform.rotation = rotation;
        prefab.SetActive(true);
    }

    // Fonction appel�e lorsque l'�tat des images r�f�renc�es change
    private void ImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Pour chaque image ajout�e, mettre � jour la position et la rotation de l'objet correspondant
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateImage(trackedImage);
        }
        // Pour chaque image mise � jour, mettre � jour la position et la rotation de l'objet correspondant
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateImage(trackedImage);
        }
        // Pour chaque image supprim�e, d�sactiver l'objet correspondant
        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            spawnedPrefabs[trackedImage.name].SetActive(false);
        }
    }
}
