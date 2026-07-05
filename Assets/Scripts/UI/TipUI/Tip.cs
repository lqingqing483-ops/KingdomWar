using UnityEngine;
using UnityEngine.UI;
namespace KingdomWar.UI
{
public class Tip : MonoBehaviour
{
    private Text text;

    private void Awake()
    {
        text = transform.Find("Text").GetComponent<Text>();
    }

    public void Init(string str)
    {
        text.text = str;
        Invoke("OnDestroy", 1.5f);
    }

    private void OnDestroy()
    {
        Destroy(this.gameObject);
    }
}

}
