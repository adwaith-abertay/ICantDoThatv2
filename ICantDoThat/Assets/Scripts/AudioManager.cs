using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UIButtonSoundManager : MonoBehaviour
{
    [System.Serializable]
    public class ButtonSound
    {
        public string buttonName;      // Name in UXML
        public AudioClip specificSound;
    }

    [Header("General Settings")]
    public UIDocument[] uiDocuments;  // All active UIs assigned here
    public AudioSource audioSource;
    public AudioClip pressSound;

    [Header("Button-Specific Sounds")]
    public ButtonSound[] buttonSounds;

    void OnEnable()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        foreach (var doc in uiDocuments)
        {
            if (doc == null) continue;
            var root = doc.rootVisualElement;

            foreach (var bs in buttonSounds)
            {
                var button = root.Q<Button>(bs.buttonName);
                if (button != null)
                {
                    button.clicked += () => OnButtonPressed(bs.specificSound);
                }
                else
                {
                    Debug.LogWarning($"Button '{bs.buttonName}' not found in {doc.name}.");
                }
            }
        }
    }

    void OnButtonPressed(AudioClip specific)
    {
        if (audioSource && pressSound)
            audioSource.PlayOneShot(pressSound);
        if (audioSource && specific)
            audioSource.PlayOneShot(specific);
    }


    void OnEnable2()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        foreach (var doc in uiDocuments)
        {
            if (doc == null) continue;
            var root = doc.rootVisualElement;
            Debug.Log($"Scanning UI Document: {doc.name}");

            foreach (var bs in buttonSounds)
            {
                Debug.Log($"Looking for button: {bs.buttonName}");
                var button = root.Q<Button>(bs.buttonName);
                if (button != null)
                {
                    Debug.Log($"✅ Found button {bs.buttonName} in {doc.name}");
                    button.clicked += () => OnButtonPressed(bs.specificSound);
                }
                else
                {
                    Debug.LogWarning($"⚠️ Button '{bs.buttonName}' not found in {doc.name}");
                }
            }
        }
    }

   

}
