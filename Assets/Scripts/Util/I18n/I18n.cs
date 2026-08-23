using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using Arcade.Compose.Dialog;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Arcade.Util.I18n
{
	public static class I18n
	{
		public const string DefaultLocaleId = "zh_Hans";

		public static string CurrentLocale { get; private set; } = DefaultLocaleId;
		public static event Action OnLocaleChanged;

		public static List<LocaleEntry> LocaleEntries { get; private set; } = new List<LocaleEntry>();

		private static Dictionary<string, string> _defaultLocaleMap = new();
		private static Dictionary<string, string> _currentLocaleMap = new();

		public static string LocaleDirectory => Path.Combine(Application.streamingAssetsPath, "locales");
		public static string LocaleListPath => Path.Combine(Application.streamingAssetsPath, "locales.json");

		public const string UnknownValue = "???";

		/// <summary>
		/// Initializes the localization system with the specified locale identifier.
		/// </summary>
		/// <remarks>
		/// This method reloads the available locale list, sets the current locale to the specified identifier,
		/// and loads the corresponding locale data so that translation lookups can be performed.
		/// </remarks>
		/// <param name="localeId">The identifier of the locale to initialize.</param>
		public static void Initialize(string localeId)
		{
			ReloadLocaleList();
			CurrentLocale = DetermineLocale(localeId).Id;

			try
			{
				if (CurrentLocale != DefaultLocaleId) _defaultLocaleMap = LoadLocale(DefaultLocaleId);
				_currentLocaleMap = LoadLocale(CurrentLocale);
				OnLocaleChanged?.Invoke();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		/// <summary>
		/// Reloads the list of available locales from the locale list JSON file.
		/// </summary>
		/// <remarks>
		/// This method reads the file at <see cref="LocaleListPath"/> and populates <see cref="LocaleEntries"/>.
		/// If the file cannot be read or parsed, the exception is logged and a dialog is shown to the user
		/// before the application exits.
		/// </remarks>
		private static void ReloadLocaleList()
		{
			try
			{
				var raw = File.ReadAllText(LocaleListPath);
				LocaleEntries = JsonConvert.DeserializeObject<List<LocaleEntry>>(raw);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		/// <summary>
		/// Sets the current locale to the specified locale identifier, resolving it against the available locales.
		/// </summary>
		/// <remarks>
		/// This method first attempts to find an exact match for <paramref name="localeId"/> in the list of locale entries.
		/// If no match is found, it falls back to the locale whose language key corresponds to the current system language.
		/// If that also fails, it falls back to the default locale. When the default locale is unavailable, an error dialog
		/// is shown and the application exits. The selected locale entry is stored in <see cref="CurrentLocale"/>.
		/// </remarks>
		/// <param name="localeId">The identifier of the locale to set as current.</param>
		public static LocaleEntry DetermineLocale(string localeId)
		{
			var localeEntry = string.IsNullOrEmpty(localeId) ? null : LocaleEntries.SingleOrDefault(x => x.Id == localeId);

			if (localeEntry == null)
			{
				// fallback to system locale
				localeEntry = LocaleEntries.FirstOrDefault(x => x.LanguageKey == Application.systemLanguage.ToString());
			}

			if (localeEntry == null)
			{
				// fallback to default locale
				try
				{
					localeEntry = LocaleEntries.Single(x => x.Id == DefaultLocaleId);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					return null;
				}
			}

			return localeEntry!;
		}

		private static Dictionary<string, string> LoadLocale(string localeId)
		{
			var path = Path.Combine(LocaleDirectory, $"{localeId}.json");

			if (!File.Exists(path))
			{
				throw new FileNotFoundException();
			}

			return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
		}

		public static void SetLocale(string localeId, bool silent = false)
		{
			var prevLocale = CurrentLocale;
			var prevLocaleMap = new Dictionary<string, string>(_currentLocaleMap);

			CurrentLocale = localeId;

			try
			{
				if (CurrentLocale != DefaultLocaleId) _defaultLocaleMap = LoadLocale(DefaultLocaleId);
				_currentLocaleMap = LoadLocale(CurrentLocale);
			}
			catch (Exception ex)
			{
				CurrentLocale = prevLocale;
				_currentLocaleMap = prevLocaleMap;

				var err = $"Failed to set locale to `{localeId}`: {ex.Message}";
				Debug.LogError(err);
				if (Application.isPlaying)
				{
					AdeBasicSingleDialogContent.Instance.Show(err, Translate("dialogs.exception.locale.read.title", "Unable to read locale"));
				}
			}

			if (!silent) OnLocaleChanged?.Invoke();
		}

		public static string Translate(string key)
		{
			if (_currentLocaleMap.Count == 0)
			{
				if (CurrentLocale != DefaultLocaleId) _defaultLocaleMap = LoadLocale(DefaultLocaleId);
				_currentLocaleMap = LoadLocale(CurrentLocale);
			}

			if (_currentLocaleMap.TryGetValue(key, out string value))
			{
				return value;
			}

			if (_defaultLocaleMap.TryGetValue(key, out value))
			{
				return value;
			}

			string err = $"Failed to resolve locale key `{key}`";
			AdeBasicSingleDialogContent.Instance.Show(err, TranslateOrDefault("dialogs.exception.locale.unknown_key.title", "Unknown i18n key"));
			Debug.LogWarning(err);
			return UnknownValue;
		}

		public static string Translate(string key, params object[] args) =>
			string.Format(Translate(key), args.Select(x => x.ToString().Replace(@"\", @"\\")).ToArray<object>());

		public static string Translate(string key, Dictionary<string, string> args) =>
			Regex.Replace(Translate(key), @"{([^}]+)}", m =>
				args.TryGetValue(m.Groups[1].Value, out var v) ? v.ToString().Replace(@"\", @"\\") : UnknownValue);

		public static string TranslateOrDefault(string key, string defaultValue)
		{
			try
			{
				return Translate(key);
			}
			catch (Exception)
			{
				return defaultValue;
			}
		}
		
		public static void SetLocaleWithoutNotify(string localeId)
		{
			CurrentLocale = localeId;
		}
	}
}
