using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class BlacklistPage : SubPage
        {

            public override string title => "Blacklisted scenes";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.assets.blacklist;

            protected override void OnAdded()
            {
                view.BindToSettings();
                SetupBlocklist(SceneManager.settings.project.m_blacklist);
            }

            public void SetupBlocklist(Blocklist setting)
            {

                var list = view.Q<ListView>();
                list.makeItem += () => new TextField();

                list.itemsAdded += (e) =>
                {
                    setting[e.First()] = Normalize(GetCurrentFolder());
                    SaveAndNotify();
                };

                list.itemsRemoved += (e) =>
                {
                    foreach (var i in e)
                        setting.RemoveAt(i);
                    SaveAndNotify();
                };

                list.bindItem += (element, i) =>
                {

                    list.ClearSelection();
                    element.userData = i;
                    element.RegisterCallback<ClickEvent>(OnClick);

                    var text = (TextField)element;
                    text.value = setting[i];
                    text.RegisterCallback<FocusOutEvent>(OnChange);
                    element.Query<BindableElement>().ForEach(e => e.SetEnabled(true));

                };

                list.unbindItem += (element, i) =>
                {

                    list.ClearSelection();

                    var text = ((TextField)element);

                    text.UnregisterCallback<ClickEvent>(OnClick);
                    text.UnregisterCallback<FocusOutEvent>(OnChange);
                    text.value = null;

                };

                view.Query<BindableElement>().ForEach(e => e.SetEnabled(true));

                void OnClick(ClickEvent e)
                {
                    var text = e.currentTarget as TextField ?? e.target as TextField;
                    var index = ((int)text.userData);
                    list.SetSelection(index);
                }

                void OnChange(FocusOutEvent e)
                {

                    var text = e.currentTarget as TextField ?? e.target as TextField;
                    var index = (int)text.userData;

                    setting[index] = Normalize(text.value);
                    SceneManager.settings.project.Save();
                    SceneImportUtility.Notify();

                }

                string GetCurrentFolder()
                {
                    var projectWindowUtilType = typeof(ProjectWindowUtil);
                    var getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
                    var obj = getActiveFolderPath.Invoke(null, Array.Empty<object>());
                    return obj.ToString() + "/";
                }

                void SaveAndNotify()
                {
                    SceneManager.settings.project.Save();
                    SceneImportUtility.Notify();
                }

                string Normalize(string path) =>
                    BlocklistUtility.Normalize(path);

            }

        }

    }

}
