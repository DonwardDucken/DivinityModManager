﻿using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Util;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace DivinityModManager.Models
{
	public interface IDivinityModData
	{
		string UUID { get; set; }
		string Name { get; set; }
		string Folder { get; set; }
		string MD5 { get; set; }
		DivinityModVersion Version { get; set; }
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class DivinityModData : ReactiveObject, IDivinityModData, ISelectable
	{
		private int index = -1;

		[JsonProperty]
		public int Index
		{
			get => index;
			set { this.RaiseAndSetIfChanged(ref index, value); }
		}

		private string filePath;

		public string FilePath
		{
			get => filePath;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref filePath, value); 
				if(!String.IsNullOrWhiteSpace(filePath))
				{
					FileName = Path.GetFileName(FilePath);
				}
			}
		}

		private string filename;

		public string FileName
		{
			get => filename;
			set { this.RaiseAndSetIfChanged(ref filename, value); }
		}

		[JsonProperty]
		public string UUID { get; set; }

		private string name;

		[JsonProperty]
		public string Name
		{
			get => name;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref name, value);
			}
		}

		[JsonProperty(PropertyName="FileName")]
		public string OutputPakName
		{
			get
			{
				if (!Folder.Contains(UUID))
				{
					return Path.ChangeExtension($"{Folder}_{UUID}", "pak");
				}
				else
				{
					return Path.ChangeExtension($"{FileName}", "pak");
				}
			}
		}


		[JsonProperty] public string Description { get; set; }
		[JsonProperty] public string Author { get; set; }
		[JsonProperty] public DivinityModVersion Version { get; set; }
		public string Folder { get; set; }
		public string MD5 { get; set; }
		[JsonProperty] public string Type { get; set; }
		[JsonProperty] public List<string> Modes { get; set; } = new List<string>();
		public string Targets { get; set; }
		public DateTime LastModified { get; set; }

		private DivinityModVersion headerVersion;
		public DivinityModVersion HeaderVersion
		{
			get => headerVersion;
			set
			{
				headerVersion = value;
				if(headerVersion != null)
				{
					IsClassicMod = headerVersion.Minor == 1;
					if (IsClassicMod)
					{
						System.Diagnostics.Trace.WriteLine($"Found a Classic mod: {Name}");
					}
				}
			}
		}

		public bool IsHidden { get; set; } = false;
		public bool IsLarianMod { get; set; } = false;

		private bool isClassicMod = false;

		public bool IsClassicMod
		{
			get => isClassicMod;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref isClassicMod, value); 
				if(value)
				{
					CanDrag = false;
				}
			}
		}

		private DivinityExtenderModStatus extenderModStatus = DivinityExtenderModStatus.NONE;

		public DivinityExtenderModStatus ExtenderModStatus
		{
			get => extenderModStatus;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref extenderModStatus, value);
				UpdateOsirisExtenderToolTip();
			}
		}

		public string OsirisExtenderSupportToolTipText { get; private set; }

		public void UpdateOsirisExtenderToolTip()
		{
			switch(ExtenderModStatus)
			{
				case DivinityExtenderModStatus.REQUIRED:
				case DivinityExtenderModStatus.REQUIRED_MISSING:
				case DivinityExtenderModStatus.REQUIRED_DISABLED:
				case DivinityExtenderModStatus.REQUIRED_OLD:
					OsirisExtenderSupportToolTipText = "";
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
					{
						OsirisExtenderSupportToolTipText = "[MISSING] ";
					}
					else if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED)
					{
						OsirisExtenderSupportToolTipText = "[EXTENSIONS DISABLED] ";
					}
					else if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_OLD)
					{
						OsirisExtenderSupportToolTipText = "[OLD] ";
					}
					if (OsiExtenderData.RequiredExtensionVersion > -1)
					{
						OsirisExtenderSupportToolTipText += $"Requires Osiris Extender v{OsiExtenderData.RequiredExtensionVersion} or higher";
					}
					else
					{
						OsirisExtenderSupportToolTipText += "Requires the Osiris Extender";
					}
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED)
					{
						OsirisExtenderSupportToolTipText += " (Enable Extensions in the OsiExtender config)";
					}
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_OLD)
					{
						OsirisExtenderSupportToolTipText += " (Update by running the game)";
					}
					break;
				case DivinityExtenderModStatus.SUPPORTS:
					if (OsiExtenderData.RequiredExtensionVersion > -1)
					{
						OsirisExtenderSupportToolTipText = $"Supports Osiris Extender v{OsiExtenderData.RequiredExtensionVersion} or higher";
					}
					else
					{
						OsirisExtenderSupportToolTipText = $"Supports the Osiris Extender";
					}
					break;
				case DivinityExtenderModStatus.NONE:
				default:
					OsirisExtenderSupportToolTipText = "";
					break;
			}
			this.RaisePropertyChanged("OsirisExtenderSupportToolTipText");
		}

		[JsonProperty] public DivinityModOsiExtenderConfig OsiExtenderData { get; set; }
		[JsonProperty] public List<DivinityModDependencyData> Dependencies { get; set; } = new List<DivinityModDependencyData>();

		private string displayName;

		public string DisplayName
		{
			get => displayName;
			set { this.RaiseAndSetIfChanged(ref displayName, value); }
		}

		public void UpdateDisplayName()
		{
			if (DisplayFileForName)
			{
				if (!IsEditorMod)
				{
					DisplayName = Path.GetFileName(FilePath);
				}
				else
				{
					DisplayName = Folder + " [Editor Project]";
				}
			}
			else
			{
				DisplayName = !IsClassicMod ? Name : Name + " [Classic]";
			}
		}

		private bool displayFileForName = false;

		public bool DisplayFileForName
		{
			get => displayFileForName;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref displayFileForName, value);
			}
		}

		private string dependenciesText;

		public string DependenciesText
		{
			get => dependenciesText;
			private set { this.RaiseAndSetIfChanged(ref dependenciesText, value); }
		}

		private bool hasDescription = false;

		public bool HasDescription
		{
			get => hasDescription;
			set { this.RaiseAndSetIfChanged(ref hasDescription, value); }
		}

		private bool hasToolTip = false;

		public bool HasToolTip
		{
			get => hasToolTip;
			set { this.RaiseAndSetIfChanged(ref hasToolTip, value); }
		}

		private bool hasDependencies = false;

		public bool HasDependencies
		{
			get => hasDependencies;
			set { this.RaiseAndSetIfChanged(ref hasDependencies, value); }
		}

		private bool hasOsirisExtenderSettings = false;

		public bool HasOsirisExtenderSettings
		{
			get => hasOsirisExtenderSettings;
			set { this.RaiseAndSetIfChanged(ref hasOsirisExtenderSettings, value); }
		}

		private bool isEditorMod = false;

		public bool IsEditorMod
		{
			get => isEditorMod;
			set { this.RaiseAndSetIfChanged(ref isEditorMod, value); }
		}

		private bool isActive = false;

		public bool IsActive
		{
			get => isActive;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref isActive, value);
				if(IsClassicMod)
				{
					CanDrag = false;
				}
			}
		}

		private bool isSelected = false;

		public bool IsSelected
		{
			get => isSelected;
			set 
			{ 
				if(value && Visibility != Visibility.Visible)
				{
					value = false;
				}
				this.RaiseAndSetIfChanged(ref isSelected, value); 
			}
		}

		private bool canDrag = true;

		public bool CanDrag
		{
			get => canDrag;
			private set { this.RaiseAndSetIfChanged(ref canDrag, value); }
		}

		private Visibility visibility = Visibility.Visible;

		public Visibility Visibility
		{
			get => visibility;
			set { this.RaiseAndSetIfChanged(ref visibility, value); }
		}

		public void UpdateDependencyText()
		{
			HasDescription = !String.IsNullOrWhiteSpace(Description);
			string t = "";
			var listDependencies = Dependencies.Where(d => !DivinityModDataLoader.IgnoreMod(d.UUID)).ToList();
			if (listDependencies.Count > 0)
			{
				HasDependencies = true;
				//t += "Dependencies" + Environment.NewLine;
				for (var i = 0; i < listDependencies.Count; i++)
				{
					var mod = listDependencies[i];
					t += $"{mod.Name} {mod.Version.Version}";
					if (i < listDependencies.Count - 1) t += Environment.NewLine;
				}
			}
			else
			{
				HasDependencies = false;
			}

			DependenciesText = t;

			HasToolTip = HasDescription | HasDependencies;
		}

		private DivinityModWorkshopData workshopData = new DivinityModWorkshopData();

		public DivinityModWorkshopData WorkshopData
		{
			get => workshopData;
			set { this.RaiseAndSetIfChanged(ref workshopData, value); }
		}

		//public DivinityModWorkshopData WorkshopData { get; private set; } = new DivinityModWorkshopData();
		public ICommand OpenWorkshopPageCommand { get; private set; }

		public void OpenSteamWorkshopPage()
		{
			if (WorkshopData != null && !String.IsNullOrEmpty(WorkshopData.ID))
			{
				string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopData.ID}";
				System.Diagnostics.Process.Start(url);
			}
		}

		public override string ToString()
		{
			return $"Mod|Name({Name}) Version({Version?.Version}) Author({Author}) UUID({UUID})";
		}

		public DivinityLoadOrderEntry ToOrderEntry()
		{
			return new DivinityLoadOrderEntry
			{
				UUID = this.UUID,
				Name = this.Name
			};
		}

		public DivinityProfileActiveModData ToProfileModData()
		{
			return new DivinityProfileActiveModData()
			{
				Folder = Folder,
				MD5 = MD5,
				Name = Name,
				UUID = UUID,
				Version = Version.VersionInt
			};
		}

		public DivinityModData()
		{
			var canOpenWorkshopLink = this.WhenAnyValue(x => x.WorkshopData.ID, (id) => !String.IsNullOrEmpty(id));
			OpenWorkshopPageCommand = ReactiveCommand.Create(OpenSteamWorkshopPage, canOpenWorkshopLink);
		}
	}
}
