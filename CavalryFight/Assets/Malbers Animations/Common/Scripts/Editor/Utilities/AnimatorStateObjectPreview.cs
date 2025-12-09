#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MalbersAnimations
{
    [CustomPreview(typeof(AnimatorState))]
    public class AnimatorStateObjectPreview : ObjectPreview
    {
        Editor _preview;
        int _animationClipId = 0;

        static FieldInfo _cachedAvatarPreviewField;
        static FieldInfo _cachedTimeControlField;
        static FieldInfo _cachedStopTimeField;

        public override void Initialize(Object[] targets)
        {
            base.Initialize(targets);
            if (targets.Length > 1 || Application.isPlaying) return;


            SourceAnimationClipEditorFields();


            AnimationClip clip = GetAnimationClip(target as AnimatorState);

            if (clip != null)
            {
                _preview = Editor.CreateEditor(clip);
                _animationClipId = clip.GetInstanceID();
            }
        }



        private AnimationClip GetAnimationClip(AnimatorState state)
        {
            return state?.motion as AnimationClip;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            CleanUpPreviewEditor();
        }

        public override bool HasPreviewGUI()
        {
            return _preview?.HasPreviewGUI() ?? false;
        }


        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            GUI.Label(r, target.name);
        }


        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnInteractivePreviewGUI(r, background);

            AnimationClip clip = GetAnimationClip(target as AnimatorState);

            if (clip != null && clip.GetInstanceID() != _animationClipId)
            {
                CleanUpPreviewEditor();
                _preview = Editor.CreateEditor(clip);
                _animationClipId = clip.GetInstanceID();
                return;
            }


            if (_preview != null)
            {
                UpdateClipEditor(_preview, clip);
                _preview.OnInteractivePreviewGUI(r, background);
            }
        }

        private void UpdateClipEditor(Editor preview, AnimationClip clip)
        {
            if (_cachedAvatarPreviewField == null || _cachedTimeControlField == null || _cachedStopTimeField == null) return;

            var avatarPreview = _cachedAvatarPreviewField.GetValue(preview);
            var timeControl = _cachedTimeControlField.GetValue(avatarPreview);

            _cachedStopTimeField.SetValue(timeControl, clip.length);
        }

        void CleanUpPreviewEditor()
        {
            if (_preview != null)
            {
                UnityEngine.Object.DestroyImmediate(_preview);
                _preview = null;
                _animationClipId = 0;
            }
        }



        private void SourceAnimationClipEditorFields()
        {
            if (_cachedAvatarPreviewField != null) return;

            _cachedAvatarPreviewField = System.Type.GetType("UnityEditor.AnimationClipEditor, UnityEditor").GetField("m_AvatarPreview", BindingFlags.NonPublic | BindingFlags.Instance);

            _cachedTimeControlField = System.Type.GetType("UnityEditor.AvatarPreview, UnityEditor").GetField("timeControl", BindingFlags.Public | BindingFlags.Instance);

            _cachedStopTimeField = System.Type.GetType("UnityEditor.TimeControl, UnityEditor").GetField("stopTime", BindingFlags.Public | BindingFlags.Instance);
        }
    }
}
#endif