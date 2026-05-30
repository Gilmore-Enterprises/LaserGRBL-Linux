//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using System;
using System.Collections.Generic;
using System.Text;

namespace LaserGRBL
{
	[Serializable()]
	public class CustomButtons
	{
		private static List<CustomButton> buttons;

		private static string UserFile { get => System.IO.Path.Combine(GrblCore.DataPath, "CustomButtons.bin"); }
		private static string StandardFile { get => System.IO.Path.Combine(LaserGRBL.GrblCore.ExePath, "StandardButtons.zbn"); }

		public static void LoadFile() //in ingresso
		{
			if (!System.IO.File.Exists(UserFile) && System.IO.File.Exists(StandardFile))
			{
				try { System.IO.File.Copy(StandardFile, UserFile, false); }
				catch { }
			}

			buttons = (List<CustomButton>)Tools.Serializer.ObjFromFile(UserFile);
			if (buttons == null)
			{
				if (buttons == null) buttons = (List<CustomButton>)Settings.GetAndDeleteObject("Custom Buttons", null);
				if (buttons == null) buttons = new List<CustomButton>();
				SaveFile();
			}
		}

		public static void SaveFile()
		{
			Tools.Serializer.ObjToFile(buttons, UserFile); //salva
		}

		internal static void Reorder(int oldindex, int newindex)
		{
			CustomButton item = buttons[oldindex];
			buttons.RemoveAt(oldindex);
			if(oldindex < newindex && newindex > 0)
			{
				newindex--; // removing the element from the list, has impact on the index
			}
			if (newindex < 0 || newindex > buttons.Count)
				buttons.Add(item);
			else
				buttons.Insert(newindex, item);

			SaveFile();
		}

		internal static void Add(CustomButton cb)
		{buttons.Add(cb);}

		public static IEnumerable<CustomButton> Buttons
		{ get { return buttons; } }

		internal static void Remove(CustomButton cb)
		{buttons.Remove(cb);}

		internal static CustomButton GetButton(int index)
		{
			if (buttons != null && buttons.Count > index)
				return buttons[index];
			else
				return null;
		}

		// UI (file dialogs) handled by caller (App layer); pass the chosen path here.
		public static void Export(string filePath)
		{
			if (!string.IsNullOrEmpty(filePath))
				Tools.Serializer.ObjToFile(buttons, filePath, Tools.Serializer.SerializationMode.Binary, null, true);
		}

		// mergeChoice: true = merge (keep existing), false = replace, null = cancelled.
		// File dialog moved to App layer (Phase 6); caller passes the resolved path.
		public static bool Import(string filename, bool? mergeChoice = true)
		{
			if (filename == null || !System.IO.File.Exists(filename) || !mergeChoice.HasValue)
				return false;

			try
			{
				List<CustomButton> list = Tools.Serializer.ObjFromFile(filename) as List<CustomButton>;
				if (list == null || list.Count == 0) return false;

				if (!mergeChoice.Value)
					buttons.Clear();

				foreach (CustomButton cb in list)
					buttons.Add(cb);

				return true;
			}
			catch { return false; }
		}

		public static int Count { get { return buttons.Count; } }
	}


	[Serializable]
	public class CustomButton
	{
		public enum EnableStyles { Always = 0, Connected = 1, Idle = 3, Run = 4, IdleProgram = 10}
		public enum ButtonTypes { Button = 0, TwoStateButton = 1, PushButton =2 }

		public System.Guid guid = Guid.NewGuid();
		// Raw PNG/BMP bytes — was System.Drawing.Image (GDI+). App layer decodes to Avalonia Bitmap.
		public byte[] ImageData;
		public string GCode;
		public string GCode2;
		public string Caption;
		public string ToolTip;

		public EnableStyles EnableStyle;
		public ButtonTypes ButtonType;

		public bool EnabledNow(GrblCore core)
		{
			if (EnableStyle == EnableStyles.Always)
				return true;
			else if (EnableStyle == EnableStyles.Connected)
				return core.IsConnected;
			else if (EnableStyle == EnableStyles.Idle)
				return core.MachineStatus == GrblCore.MacStatus.Idle;
			else if (EnableStyle == EnableStyles.Run)
				return core.MachineStatus == GrblCore.MacStatus.Run;
			else if (EnableStyle == EnableStyles.IdleProgram)
				return core.MachineStatus == GrblCore.MacStatus.Idle && core.HasProgram;

			return false;
		}
	}
}
