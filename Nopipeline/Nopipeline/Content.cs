﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nopipeline
{
	/// <summary>
	/// MGCB config object. Everything is merged in here.
	/// </summary>
	public class Content
	{
		/// <summary>
		/// Root directory of all content. 
		/// A 'root' property in the npl config which 
		/// gets appended to all 'path' properties.
		/// Unlike 'path', can contain '../'
		/// </summary>
		public static string Root = "";

		/// <summary>
		/// All the global settings.
		/// TODO: Add support in the NPL config.
		/// </summary>
		private StringBuilder _globalSettings = new StringBuilder();

		/// <summary>
		/// Referenced libraries.
		/// </summary>
		private HashSet<string> _references = new HashSet<string>();

		/// <summary>
		/// Actual content items.
		/// </summary>
		public Dictionary<string, Item> ContentItems = new Dictionary<string, Item>();


		public string Build()
		{
			var builder = new StringBuilder();

			builder.AppendLine();
			builder.AppendLine(ContentStructure.GlobalPropertiesDivider);
			builder.AppendLine();

			builder.Append(_globalSettings);

			builder.AppendLine();
			builder.AppendLine(ContentStructure.ReferencesDivider);
			builder.AppendLine();

			// References.
			foreach (var reference in _references)
			{
				builder.AppendLine(ContentStructure.ReferenceKeyword + reference);
			}

			builder.AppendLine();
			builder.AppendLine(ContentStructure.ContentDivider);
			builder.AppendLine();

			// Items.
			foreach (var item in ContentItems.Values)
			{
				builder.Append(item.ToString());
			}

			RemoveTrailingBlankLines(builder);

			return builder.ToString();
		}

		public void AddGlobalSetting(string setting) =>
			_globalSettings.AppendLine(setting);


		public void AddContentItem(Item item)
		{
			if (ContentItems.ContainsKey(item.Path))
			{
				ContentItems[item.Path] = item;
			}
			else
			{
				ContentItems.Add(item.Path, item);
			}
		}


		public void AddReference(string reference)
		{
			var normalizedReference = reference.Replace("\\", "/");
			if (!_references.Contains(normalizedReference))
			{
				_references.Add(normalizedReference);
			}
		}


		/// <summary>
		/// Checks if content files exist and checks watched files.
		/// </summary>
		public void CheckIntegrity(WatchSnapshot snapshot)
		{
			Console.WriteLine("Checking integrity of the final config.");
			Console.WriteLine();

			var checkedItems = new Dictionary<string, Item>();

			foreach (Item item in ContentItems.Values)
			{
				var fullItemPath = Path.Combine(Root, item.Path);
				Console.WriteLine("Checking " + fullItemPath);

				if (File.Exists(fullItemPath))
				{
					checkedItems.Add(item.Path, item);

					// Watched files are files which aren't tracked by the content pipeline.
					// But they are tracked by us! We look at the watched files and, if any of them were added, deleted or modified,
					// we "modify" the tracked file by changing its Last Modified date. This way 
					// Pipeline thinks the file has been updated and rebuilds it.
					if (!snapshot.CheckMatch(item))
					{
						Console.WriteLine("Watched filed were modified! Updating: " + item.Path);
						File.SetLastWriteTime(Path.Combine(Root, item.Path), DateTime.Now);
					}
				}

			}
			ContentItems = checkedItems;

			Console.WriteLine();

			var checkedReferences = new HashSet<string>();
			foreach (var reference in _references)
			{
				Console.WriteLine("Checking reference: " + Path.Combine(Root, reference));
				if (File.Exists(Path.Combine(Root, reference)))
				{
					checkedReferences.Add(reference);
				}
				else
				{
					Console.WriteLine(reference + " wasn't found! Deleting it from the config.");
				}
			}
			_references = checkedReferences;

		}


		private void RemoveTrailingBlankLines(StringBuilder builder)
		{
			while (builder.ToString().EndsWith(Environment.NewLine))
			{
				builder.Remove(builder.Length - Environment.NewLine.Length, Environment.NewLine.Length);
			}
		}

	}
}
