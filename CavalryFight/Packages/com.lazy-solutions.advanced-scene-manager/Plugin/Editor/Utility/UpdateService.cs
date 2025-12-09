using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Utility;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AdvancedSceneManager.Services
{

    /// <summary>Provides functionality for checking, downloading, and managing updates for ASM.</summary>
    interface IUpdateService
    {

        /// <summary>Gets the currently installed ASM version.</summary>
        Version installedVersion { get; }

        /// <summary>Gets the latest available ASM version, as retrieved from GitHub.</summary>
        Version latestVersion { get; }

        /// <summary>Gets the patch notes text associated with the latest retrieved version.</summary>
        string latestPatchNotes { get; }

        /// <summary>Gets the timestamp of the last successful update check.</summary>
        DateTime? lastChecked { get; }

        /// <summary>Gets whether a newer ASM version is available for download.</summary>
        bool isUpdateAvailable { get; }

        /// <summary> Gets whether the latest update must be downloaded from the Asset Store (for example, when the major or minor version has changed).</summary>
        bool requiresAssetStoreUpdate { get; }

        /// <summary>Checks for available ASM updates from GitHub and updates cached version and patch note information. </summary>
        /// <param name="logError">Whether to log detailed exceptions to the console if an error occurs.</param>
        /// <param name="token">Optional cancellation token used to abort the request.</param>
        /// <returns><see langword="true"/> if the check completed successfully; otherwise, <see langword="false"/>.</returns>
        Awaitable<bool> CheckForUpdatesAsync(bool logError = false, CancellationToken? token = null);

        /// <summary>Downloads and applies the latest update package from GitHub or redirects to the Asset Store if required.</summary>
        /// <remarks>Disabled in #ASM_DEV, log message is outputted instead after downloading.</remarks>
        Awaitable ApplyUpdateAsync();

        /// <summary>Opens the ASM GitHub releases page in the default browser.</summary>
        void OpenReleasesPage();

        /// <summary>Marks the current latest version as having been acknowledged by the user, preventing further notifications for this version.</summary>
        void MarkNotified();

    }

    [RegisterService(typeof(IUpdateService))]
    sealed class UpdateService : ServiceBase, IUpdateService
    {

        readonly HttpClient client = new();

        const string githubReleases = "https://github.com/Lazy-Solutions/AdvancedSceneManager/releases/latest";
        const string githubPatchFile = "https://gist.githubusercontent.com/Zumwani/195afd3053cf1cb951013e30908903c0/raw";
        const string githubReleasesAPI = "https://api.github.com/repos/Lazy-Solutions/AdvancedSceneManager/releases/latest";
        readonly string cachePath = Path.Combine(Application.temporaryCachePath, "ASM", "Updates");

        protected override void OnInitialize()
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "request");
            StartAutoUpdateCheck();
            DeleteOldUpdatePackages();
        }

        protected override void OnDispose() =>
            client.Dispose();

        public void OpenReleasesPage() =>
            Application.OpenURL(githubReleases);

        public void MarkNotified()
        {
            SceneManager.settings.user.lastPatchWhenNotified = latestVersion?.ToString();
            SceneManager.settings.user.Save();
        }

        void DeleteOldUpdatePackages()
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, true);
        }

        #region Properties

        public Version installedVersion =>
            _installedVersion ??= Version.Parse(SceneManager.package.version);
        Version _installedVersion;

        public Version latestVersion =>
            _latestVersion ??= (Version.TryParse(SceneManager.settings.user.cachedLatestVersion, out var v) ? v : null);
        Version _latestVersion;

        public string latestPatchNotes =>
            SceneManager.settings.user.cachedPatchNotes;

        public DateTime? lastChecked =>
            DateTime.TryParse(SceneManager.settings.user.lastUpdateCheck, out var dt) ? dt : null;

        public bool isUpdateAvailable =>
            latestVersion != null && latestVersion > installedVersion;

        public bool requiresAssetStoreUpdate =>
            latestVersion != null && latestVersion > installedVersion &&
            (latestVersion.Major > installedVersion.Major || latestVersion.Minor > installedVersion.Minor);

        #endregion
        #region Background check

        void StartAutoUpdateCheck() =>
            CoroutineUtility.Timer(CheckUpdateIfCooldownOver, TimeSpan.FromHours(1), description: "Update check");

        void CheckUpdateIfCooldownOver()
        {
            var user = SceneManager.settings.user;
            var project = SceneManager.settings.project;

            if (user.updateInterval == Models.UpdateInterval.Never)
                return;

            if (user.updateInterval == Models.UpdateInterval.Auto && !project.allowUpdateCheck)
                return;

            if (IsCooldownOver())
                _ = CheckForUpdatesAsync();
        }

        bool IsCooldownOver()
        {
            if (!DateTime.TryParse(SceneManager.settings.user.lastUpdateCheck, out var lastCheckDate))
                return true;

            TimeSpan? cooldownPeriod = SceneManager.settings.user.updateInterval switch
            {
                Models.UpdateInterval.Auto => TimeSpan.FromHours(3),
                Models.UpdateInterval.EveryHour => TimeSpan.FromHours(1),
                Models.UpdateInterval.Every3Hours => TimeSpan.FromHours(3),
                Models.UpdateInterval.Every6Hours => TimeSpan.FromHours(6),
                Models.UpdateInterval.Every12Hours => TimeSpan.FromHours(12),
                Models.UpdateInterval.Every24Hours => TimeSpan.FromDays(1),
                Models.UpdateInterval.Every48Hours => TimeSpan.FromDays(2),
                Models.UpdateInterval.EveryWeek => TimeSpan.FromDays(7),
                _ => null
            };

            return cooldownPeriod.HasValue && DateTime.Now - lastCheckDate >= cooldownPeriod;
        }

        #endregion
        #region Check update

        public async Awaitable<bool> CheckForUpdatesAsync(bool logError = false, CancellationToken? token = null)
        {

            SceneManager.settings.user.lastUpdateCheck = DateTime.Now.ToString();
            Log.Info("Checking for update...");

            try
            {

                var url = $"{githubPatchFile}?t={DateTime.UtcNow.Ticks}";
                var message = await client.GetAsync(url, token ?? CancellationToken.None);
                if (token?.IsCancellationRequested ?? false)
                    throw new TaskCanceledException();

                var text = await message.Content.ReadAsStringAsync();
                if (token?.IsCancellationRequested ?? false)
                    throw new TaskCanceledException();

                if (!text.Contains("\n"))
                    throw new Exception("Could not parse version file.");

                var versionStr = text[..text.IndexOf("\n")];
                var patchNotes = text[text.IndexOf("\n")..];

                if (!Version.TryParse(versionStr, out _))
                    throw new Exception("Could not parse version file.");

                SceneManager.settings.user.cachedLatestVersion = versionStr;
                SceneManager.settings.user.cachedPatchNotes = patchNotes;
                _latestVersion = null;

                SceneManager.events.InvokeCallbackSync<UpdateCheckedEvent>();
                Log.Info($"Latest update found: {latestVersion}");

                return true;

            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                if (logError)
                {
                    Debug.LogError("An error occurred when checking for updates. Exception will be logged below.");
                    Debug.LogException(e);
                }
                return false;
            }

        }

        #endregion
        #region Apply update

        public async Awaitable ApplyUpdateAsync()
        {
            try
            {
                if (requiresAssetStoreUpdate)
                {
                    UnityEditor.PackageManager.UI.Window.Open(SceneManager.package.id);
                    return;
                }

                var path = await FindAndDownloadPackage();

#if !ASM_DEV
                UnityEditor.AssetDatabase.ImportPackage(path, interactive: true);
#else
                Log.Info($"AssetDatabase.ImportPackage({path}, interactive: true);");
#endif
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        async Awaitable<string> FindAndDownloadPackage()
        {

            var response = await client.GetAsync(githubReleasesAPI);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var latestRelease = JsonUtility.FromJson<Release>(json);

            var unityPackageUrl = latestRelease.assets.FirstOrDefault(a => a.name.EndsWith(".unitypackage")).browser_download_url;
            if (string.IsNullOrEmpty(unityPackageUrl))
                throw new InvalidOperationException("No .unitypackage file found in the latest release.");

            var stream = await client.GetStreamAsync(unityPackageUrl);

            if (!Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);

            var path = Path.Combine(cachePath, $"ASM.{latestVersion}.partial.unitypackage");
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fs);

            return path;
        }

        [Serializable]
        struct Release
        {
            public Asset[] assets;
        }

        [Serializable]
        struct Asset
        {
            public string name;
            public string browser_download_url;
        }

        #endregion
    }

}
