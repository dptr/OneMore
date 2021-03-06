﻿//************************************************************************************************
// Copyright © 2018 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn
{
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Xml.Linq;


	internal class AlterSizeCommand : Command
	{
		private XElement page;
		private int delta;
		private bool selected;


		public AlterSizeCommand () : base()
		{
		}


		public void Execute (int delta)
		{
			try
			{
				using (var manager = new ApplicationManager())
				{
					page = manager.CurrentPage();
					if (page != null)
					{
						this.delta = delta;

						// determine if range is selected or entire page

						var ns = page.GetNamespaceOfPrefix("one");

						selected = page.Element(ns + "Outline").Descendants(ns + "T")
							.Where(e => e.Attributes("selected").Any(a => a.Value.Equals("all")))
							.Any(e => e.GetCData().Value.Length > 0);

						//System.Diagnostics.Debugger.Launch();

						var count = AlterByName();
						count += AlterElementsByValue();
						count += AlterCDataByValue();

						if (count > 0)
						{
							manager.UpdatePageContent(page);
						}
					}
				}
			}
			catch (Exception exc)
			{
				logger.WriteLine("Error executing AlterSizeCommand", exc);
			}
		}


		/*
		 * <one:QuickStyleDef index="1" name="p" fontColor="automatic" highlightColor="automatic" font="Calibri" fontSize="12.0" spaceBefore="0.0" spaceAfter="0.0" />
		 * <one:QuickStyleDef index="2" name="h2" fontColor="#2E75B5" highlightColor="automatic" font="Calibri" fontSize="14.0" spaceBefore="0.0" spaceAfter="0.0" />
		 */

		private int AlterByName()
		{
			int count = 0;

			// find all elements that have an attribute named fontSize, e.g. QuickStyleDef or Bullet

			var elements = page.Descendants()
				.Where(p =>
					p.Attribute("name")?.Value != "PageTitle" &&
					p.Attribute("fontSize") != null &&
					(selected == (p.Attribute("selected")?.Value == "all")));

			if (elements?.Any() == true)
			{
				foreach (var element in elements)
				{
					if (element != null)
					{
						var attr = element.Attribute("fontSize");
						if (attr != null)
						{
							if (double.TryParse(attr.Value, out var size))
							{
								attr.Value = (size + delta).ToString("#0.0");
								count++;
							}
						}
					}
				}
			}

			return count;
		}


		private int AlterElementsByValue()
		{
			int count = 0;

			var elements = page.Descendants()
				.Where(p =>
					p.Parent.Name.LocalName != "Title" &&
					p.Attribute("style")?.Value.Contains("font-size:") == true);

			if (selected)
			{
				elements = elements.Where(e => e.Attribute("selected") != null);
			}

			if (elements?.Any() == true)
			{
				foreach (var element in elements)
				{
					if (UpdateSpanStyle(element))
					{
						count++;
					}
				}
			}

			return count;
		}


		private int AlterCDataByValue()
		{
			int count = 0;

			var nodes = page.DescendantNodes().OfType<XCData>()
				.Where(n => n.Value.Contains("font-size:"));

			if (selected)
			{
				// parent one:T
				nodes = nodes.Where(n => n.Parent.Attribute("selected").Value == "all");
			}

			if (nodes?.Any() == true)
			{
				foreach (var cdata in nodes)
				{
					var wrapper = cdata.GetWrapper();

					var spans = wrapper.Elements("span")
						.Where(e => e.Attribute("style")?.Value.Contains("font-size") == true);

					if (spans?.Any() == true)
					{
						foreach (var span in spans)
						{
							if (UpdateSpanStyle(span))
							{
								// unwrap our temporary <cdata>
								cdata.Value = wrapper.GetInnerXml();
								count++;
							}
						}
					}
				}
			}

			return count;
		}


		private bool UpdateSpanStyle(XElement span)
		{
			bool updated = false;

			var attr = span.Attribute("style");
			if (attr != null)
			{
				var properties = attr.Value.Split(';')
					.Select(p => p.Split(':'))
					.ToDictionary(p => p[0], p => p[1]);

				if (properties?.ContainsKey("font-size") == true)
				{
					properties["font-size"] =
						(ParseFontSize(properties["font-size"]) + delta).ToString("#0.0") + "pt";

					attr.Value =
						string.Join(";", properties.Select(p => p.Key + ":" + p.Value).ToArray());

					updated = true;
				}
			}

			return updated;
		}


		private double ParseFontSize(string size)
		{
			var match = Regex.Match(size, @"^([0-9]+(?:\.[0-9]+)?)(?:pt){0,1}");
			if (match.Success)
			{
				size = match.Groups[match.Groups.Count - 1].Value;
				if (!string.IsNullOrEmpty(size))
				{
					return double.Parse(size);
				}
			}

			return StyleBase.DefaultFontSize;
		}
	}
}
