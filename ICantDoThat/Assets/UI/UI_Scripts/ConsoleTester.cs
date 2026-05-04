using UnityEngine;
using UnityEngine.InputSystem;

public class ConsoleTester : MonoBehaviour
{
    void Update()
    {
        // Safety check
        if (Keyboard.current == null) return;

        // Press 1: Test the Captain (Should pull image 1.3 - Angry/Fearful)
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ConsoleDatabase.Instance.TriggerMessage("VOICE_CPT_REA_FEAR");
        }

        // Press 2: Test the Soldier (Should pull image 2.2 - Happy/Confident)
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ConsoleDatabase.Instance.TriggerMessage("VOICE_SLD_AMB_02");
        }

        // Press 3: Test the Injured Soldier (Should pull image 3.1 - Fearful)
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            ConsoleDatabase.Instance.TriggerMessage("VOICE_INJ_AMB_04"); // "Mommy!"
        }
    }
}