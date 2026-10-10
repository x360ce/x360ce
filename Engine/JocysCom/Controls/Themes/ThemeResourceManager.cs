#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.Themes
{
	/// <summary>
	/// Hands out the images of a class generated from a resource file in the theme in use, at the screen's scale.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The designer code and the program take their images from the class, Properties.Resources for one, and
	/// Windows Forms draws each at the size it was made, 16 pixels for most, in either theme. Put in place of
	/// the class's own manager before the first window is made (<see cref="Install"/>), this one also looks each
	/// image up in a second resource file of versions. For an image named "add_16x16", "add_16x16_dark" is the
	/// icon for the dark theme, and "add_24x24" or "add_32x32_dark" are versions drawn at larger sizes. An image
	/// whose name carries no size, such as "Button_A", has versions named by adding theirs, such as
	/// "Button_A_30x30". The image handed out is the theme's, at <see cref="ControlsHelper.DpiScale"/> times its
	/// size, made from the version drawn nearest that size (<see cref="ControlsHelper.SetDrawnSizes"/>).
	/// </para>
	/// <para>
	/// When the theme changes, the images on the open windows are swapped for the new theme's. Images a program
	/// keeps elsewhere, in an image list for one, are for it to fetch again on <see cref="FormsTheme.ThemeChanged"/>;
	/// <see cref="Themed"/> gives the new theme's image in place of one handed out before.
	/// </para>
	/// </remarks>
	public class ThemeResourceManager : ResourceManager
	{
		/// <summary>Puts a manager in place of a resource class's own. Call it before the class's first image is asked for.</summary>
		/// <param name="resources">The class generated from the resource file, Properties.Resources for one.</param>
		/// <param name="versionsName">
		/// Name of the resource file with the dark versions and the versions drawn at larger sizes, such as
		/// "MyProgram.Properties.Icons". Without that file the images are only scaled.
		/// </param>
		public static ThemeResourceManager Install(Type resources, string versionsName)
		{
			if (resources is null)
				throw new ArgumentNullException(nameof(resources));
			const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			var field = resources.GetField("resourceMan", flags);
			var source = resources.GetProperty("ResourceManager", flags)?.GetValue(null, null) as ResourceManager;
			if (field is null || source is null)
				throw new ArgumentException("Not a class generated from a resource file: " + resources.FullName, nameof(resources));
			var manager = new ThemeResourceManager(source.BaseName, resources.Assembly, versionsName);
			field.SetValue(null, manager);
			FormsTheme.ThemeChanged += manager.FormsTheme_ThemeChanged;
			return manager;
		}

		/// <summary>The icons of this library's own controls, in the theme in use, at the screen's scale.</summary>
		/// <remarks>
		/// Each icon is listed in the resource file beside this class with its dark version and the versions
		/// drawn at larger sizes, such as "refresh_16x16", "refresh_16x16_dark" and "refresh_24x24". The
		/// controls take their icons from here rather than from their own designer files, so a program shows
		/// them in its theme without installing anything.
		/// </remarks>
		public static ThemeResourceManager Library => _Library.Value;

		static readonly Lazy<ThemeResourceManager> _Library = new Lazy<ThemeResourceManager>(() =>
		{
			var type = typeof(ThemeResourceManager);
			var manager = new ThemeResourceManager(type.FullName, type.Assembly, type.FullName);
			FormsTheme.ThemeChanged += manager.FormsTheme_ThemeChanged;
			return manager;
		});

		ThemeResourceManager(string baseName, Assembly assembly, string versionsName)
			: base(baseName, assembly)
		{
			if (!string.IsNullOrEmpty(versionsName) && assembly.GetManifestResourceNames().Contains(versionsName + ".resources"))
				_Versions = new ResourceManager(versionsName, assembly);
		}

		/// <summary>The dark versions and the versions drawn at larger sizes, or null.</summary>
		readonly ResourceManager _Versions;

		/// <summary>What was handed out, by theme and name: a resource file makes a new image on every request.</summary>
		readonly Dictionary<string, object> _Handed = new Dictionary<string, object>();

		/// <summary>The resource name of every image handed out, by the original it was made from.</summary>
		readonly ConditionalWeakTable<Image, string> _Names = new ConditionalWeakTable<Image, string>();

		/// <summary>The sizes a version may be drawn at, as multiples of the size of the image.</summary>
		static readonly float[] Multiples = { 1.25f, 1.5f, 1.75f, 2f, 2.5f, 3f, 4f };

		/// <summary>A name that ends in the image's size, such as "add_16x16".</summary>
		static readonly Regex SizedName = new Regex(@"^(?<name>.+)_(?<width>\d+)x(?<height>\d+)$");

		/// <inheritdoc/>
		public override object GetObject(string name)
		{
			return GetObject(name, null);
		}

		/// <inheritdoc/>
		public override object GetObject(string name, CultureInfo culture)
		{
			var dark = FormsTheme.IsDark;
			var key = (dark ? "dark:" : "light:") + name;
			lock (_Handed)
			{
				object value;
				if (_Handed.TryGetValue(key, out value))
					return value;
				var suffix = "_dark";
				value = dark ? GetVersion(name + suffix, culture) : null;
				if (value == null)
				{
					suffix = "";
					value = base.GetObject(name, culture);
				}
				if (value is Image original)
				{
					string known;
					if (!_Names.TryGetValue(original, out known))
					{
						ControlsHelper.SetDrawnSizes(original, GetDrawnSizes(name, original.Size, suffix, culture));
						_Names.Add(original, name);
					}
					value = ControlsHelper.ScaleImage(original);
				}
				_Handed.Add(key, value);
				return value;
			}
		}

		/// <summary>The image of the theme in use in place of one handed out before, at the same size.</summary>
		/// <returns>The image itself when it did not come from here or is already the theme's.</returns>
		public Image Themed(Image image)
		{
			var original = ControlsHelper.GetOriginal(image);
			string name;
			if (original is null || !_Names.TryGetValue(original, out name))
				return image;
			var current = ControlsHelper.GetOriginal(GetObject(name) as Image);
			return current is null || ReferenceEquals(current, original)
				? image
				: ControlsHelper.ScaleImage(current, image.Size);
		}

		Image GetVersion(string name, CultureInfo culture)
		{
			return _Versions?.GetObject(name, culture) as Image;
		}

		/// <summary>The versions of an image drawn at larger sizes, for the theme the suffix names.</summary>
		/// <param name="name">The image's name: "add_16x16", whose versions take the place of its size, or "Button_A", whose versions add theirs.</param>
		/// <param name="size">The image's own size, for a name that carries none.</param>
		/// <param name="suffix">"_dark" for the dark theme's versions, or empty.</param>
		/// <param name="culture">The culture to look the versions up in.</param>
		Image[] GetDrawnSizes(string name, Size size, string suffix, CultureInfo culture)
		{
			if (_Versions is null)
				return new Image[0];
			var stem = name;
			var match = SizedName.Match(name);
			if (match.Success)
			{
				stem = match.Groups["name"].Value;
				size = new Size(
					int.Parse(match.Groups["width"].Value, CultureInfo.InvariantCulture),
					int.Parse(match.Groups["height"].Value, CultureInfo.InvariantCulture));
			}
			return Multiples
				.Select(m => GetVersion(stem + "_" + (int)Math.Round(size.Width * m) + "x" + (int)Math.Round(size.Height * m) + suffix, culture))
				.Where(x => x != null)
				.ToArray();
		}

		void FormsTheme_ThemeChanged(object sender, EventArgs e)
		{
			foreach (var form in Application.OpenForms.Cast<Form>().ToArray())
				ControlsHelper.ReplaceImages(form, Themed);
		}
	}
}
