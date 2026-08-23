namespace Arcade.Util.I18n
{
	public record LocaleEntry
	{
		public string Id { get; set; }
		public string LanguageKey { get; set; }
		public string LocalizedName { get; set; }
	}
}
