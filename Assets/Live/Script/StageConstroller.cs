using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class StageConstroller : MonoBehaviour
{
    [SerializeField] PlayableDirector PlayableDirector = null;

    void Start()
    {
        if (PlayableDirector != null)
        {
            // エンジンがAudioを出す準備が完了するのを待つために１フレームだけ再生を遅らせる
            // これがないとアニメーションと音がずれる
            StartCoroutine(PlayAfterOneFrame());
        }
    }

    IEnumerator PlayAfterOneFrame()
    {
        yield return null;
        PlayableDirector.Play();
    }
}
