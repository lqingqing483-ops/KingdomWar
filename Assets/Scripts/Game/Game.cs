using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.UI;
namespace KingdomWar.Game
{
public class Game : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //UIManager.Instance.PushPanel(UIPanelType.mainPanel);
        UIManager.Instance.Init(); 
    }
}

}
