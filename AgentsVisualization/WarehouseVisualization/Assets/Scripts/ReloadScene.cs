using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadScene : MonoBehaviour
{
    public void LoadScene()
    {
        GameObject.Find("AgentController").GetComponent<AgentController>().InitialConfiguration();
        // SceneManager.LoadScene("RandomAgents");
    }
}
