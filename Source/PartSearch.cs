using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PartSearch
{
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class PartSearch : MonoBehaviour
	{
		public static PartCategories[] Categories;
		bool categorySetToDefault = true;

		public bool HasLoaded = false;

		public EditorPartListFilter Filter;
		public string currentSearch = "";

		public bool IsSearching
		{
			get
			{
				return (currentSearch != null && currentSearch != "" && EditorLogic.fetch.ship.Count > 0);
			}
		}

		void Init()
		{
			//set default categories
			if (Categories != null)
			{
				Categories = new PartCategories[PartLoader.LoadedPartsList.Count];
				for (int i = 0; i < PartLoader.LoadedPartsList.Count; i++)
				{
					var part = PartLoader.LoadedPartsList [i];
					Categories [i] = part.category;
				}
			}

			Filter = new EditorPartListFilter ("PartSearch_ID", DoesPartMeetSearchRequirements, "None found");
			EditorPartList.Instance.ExcludeFilters.AddFilter (Filter);
			EditorPartList.Instance.Refresh ();

			//make part icons not spin anymore
			//fixes an annoying bug where the parts will spin halfway, then stop
			EditorPartList.Instance.iconOverSpin = 0f;

			searchBarRect = new Rect (54, Screen.height - 90, 142, 20);

			greyedOutLabel = new GUIStyle (skin.label);
			greyedOutLabel.normal.textColor = new Color (0.6f, 0.6f, 0.6f);
			greyedOutLabel.active.textColor = new Color (0.6f, 0.6f, 0.6f);
			greyedOutLabel.focused.textColor = new Color (0.6f, 0.6f, 0.6f);
			greyedOutLabel.hover.textColor = new Color (0.9f, 0.9f, 0.9f);

			greyedOutLabelRect = new Rect (61, Screen.height - 87, 141, 20);

			StartCoroutine (Refresh ());
		}
		void OnDestroy()
		{
			StopCoroutine (Refresh ());
			SetCategories (true);
		}

		void Update()
		{
			if(!HasLoaded && EditorLogic.fetch != null && EditorPartList.Instance && EditorLogic.fetch.ship.Count > 0)
			{
				HasLoaded = true;
				Init ();
			}
		}

		GUISkin skin = HighLogic.Skin;
		GUIStyle greyedOutLabel;
		Rect searchBarRect;
		Rect greyedOutLabelRect;

		void OnGUI()
		{
			GUI.skin = skin;
			if (HasLoaded)
			{
				currentSearch = GUI.TextField (searchBarRect, currentSearch);
				if (!IsSearching)
					GUI.Label (greyedOutLabelRect, "Part Search", greyedOutLabel);
			}
		}

		void LateUpdate()
		{
			if (!HasLoaded)
				return;

			if (EditorLogic.fetch.ship.Count > 0)
			{
				if (IsSearching && categorySetToDefault)
					SetCategories (false);
				else if (!IsSearching && !categorySetToDefault)
					SetCategories (true);
			}
		}

		IEnumerator<YieldInstruction> Refresh()
		{
			while (true)
			{
				if(IsSearching)
					EditorPartList.Instance.Refresh ();
				yield return new WaitForSeconds (1.0f);
			}
		}

		public void SetCategories(bool toDefault)
		{
			if (toDefault)
			{
				//set back to default categories
				for(int i = 0; i < PartLoader.LoadedPartsList.Count; i++)
				{
					var part = PartLoader.LoadedPartsList [i];
					part.category = Categories [i];
				}
				//show all tabs
				EditorPartList.Instance.ShowTabs ();

				EditorPartList.Instance.Refresh ();
				categorySetToDefault = true;
			}
			else if (!toDefault)
			{
				//set all part categories to Pods
				foreach (var part in PartLoader.LoadedPartsList.Where(p => p.category != PartCategories.none))
					part.category = PartCategories.Pods;

				//hide all tabs but Pods
				EditorPartList.Instance.ForceSelectTab (PartCategories.Pods);
				EditorPartList.Instance.HideTabs ();
				EditorPartList.Instance.ShowTab (PartCategories.Pods);

				EditorPartList.Instance.Refresh ();
				categorySetToDefault = false;
			}
		}

		public bool DoesPartMeetSearchRequirements(AvailablePart part)
		{
			if (!IsSearching)
				return true;
			else
			{
				//search for modules, resources, manufacturer, and name of part
				if (
					part != null &&
					(
						(part.title != null && part.title.ToLower().Contains (currentSearch.ToLower())) ||
						(part.name != null && part.name.ToLower().Contains (currentSearch.ToLower())) ||
						(part.moduleInfos.Where (m => m.moduleName != null).Where (m => m.moduleName.ToLower().Contains (currentSearch.ToLower())).Count() > 0) ||
						(part.resourceInfos.Where (r => r.resourceName != null).Where (r => r.resourceName.ToLower().Contains (currentSearch.ToLower())).Count() > 0) ||
						(part.manufacturer != null && part.manufacturer.ToLower().Contains(currentSearch.ToLower()))
					)
				)
					return true;
				else
					return false;
			}
		}
	}
}

