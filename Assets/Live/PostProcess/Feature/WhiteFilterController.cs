using UnityEngine;
using srp.postprocess;

public class WhiteFilterController : MonoBehaviour
{
    [SerializeField] CWhiteFilter WhiteFilter = null;
    [SerializeField][Range(0.0f, 1.0f)] float Rate = 0.0f;

    private void OnDestroy()
    {
        WhiteFilter.SetRate(0.0f);
    }

    private void Start()
    {
        WhiteFilter.SetRate(1.0f);
    }

    void Update()
    {
        WhiteFilter.SetRate(Rate);
    }
}
