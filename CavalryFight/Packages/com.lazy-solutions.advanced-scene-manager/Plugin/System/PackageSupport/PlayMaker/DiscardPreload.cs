#if PLAYMAKER

using System.Collections;
using HutongGames.PlayMaker;

namespace AdvancedSceneManager.PackageSupport.PlayMaker
{

    [Tooltip("Cancels current preloaded scene.")]
    public class CancelPreload : ASMAction
    {

        protected override IEnumerator RunCoroutine()
        {
            operation = SceneManager.runtime.CancelPreload();
            yield return operation;
            Finish();
        }

    }

}

#endif
