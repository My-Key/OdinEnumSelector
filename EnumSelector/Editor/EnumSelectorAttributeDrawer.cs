using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class EnumSelectorAttributeDrawer<T> : OdinAttributeDrawer<EnumSelectorAttribute, T> where T : Enum
{
	private GUIContent m_buttonContent = new GUIContent();
	private SdfIconType m_sdfIcon;
	private ValueResolver<IEnumerable<T>> m_valuesGetter;
	
	private bool IsFlagEnum { get; set; }

	protected override void Initialize()
	{
		m_valuesGetter = ValueResolver.Get<IEnumerable<T>>(Property, Attribute.ValuesGetter);
		
		LimitValueToOptions();
		UpdateButtonContent();

		IsFlagEnum = typeof(T).GetAttribute<FlagsAttribute>() != null;
	}

	private void UpdateButtonContent()
	{
		m_buttonContent.text =
			!EditorGUI.showMixedValue ? CustomEnumSelector<T>.GetValueStringReflection(ValueEntry.SmartValue, out m_sdfIcon) : "-";
	}

	protected override void DrawPropertyLayout(GUIContent label)
	{
		m_valuesGetter.DrawError();
		
		var rect = EditorGUILayout.GetControlRect(label != null);

		if (label == null)
			rect = EditorGUI.IndentedRect(rect);
		else
			rect = EditorGUI.PrefixLabel(rect, label);

		var options = LimitValueToOptions();
		
		var output = CustomEnumSelector<T>.DrawSelectorDropdown(rect, m_buttonContent, 
			buttonRect =>
			{
				var selector =  new CustomEnumSelector<T>() { PossibleOptions = options };
				selector.ShowInPopup(buttonRect.position + Vector2.up * buttonRect.height);
				selector.SetSelection(ValueEntry.SmartValue);
				return selector;
			}, true, null, m_sdfIcon);

		if (output == null || !output.Any())
			return;

		ValueEntry.SmartValue = output.First();
		UpdateButtonContent();
	}

	private IEnumerable<T> LimitValueToOptions()
	{
		if (m_valuesGetter.HasError)
			return null;
		
		var options = m_valuesGetter.GetValue();

		if (IsFlagEnum)
		{
			long allBitsFromOptions = 0;

			foreach (var option in options)
				allBitsFromOptions |= Convert.ToInt64(option);

			ValueEntry.SmartValue =
				(T)Enum.ToObject(typeof(T), Convert.ToInt64(ValueEntry.SmartValue) & allBitsFromOptions);
		}
		else
		{
			if (!options.Contains(ValueEntry.SmartValue))
				ValueEntry.SmartValue = options.FirstOrDefault();
		}

		return options;
	}

	public class CustomEnumSelector<TEnum> : EnumSelector<TEnum> where TEnum : Enum
	{
		public IEnumerable<TEnum> PossibleOptions { get; set; }

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			base.BuildSelectionTree(tree);

			if (PossibleOptions != null)
				tree.MenuItems.RemoveAll(x =>
					!PossibleOptions.Contains(((EnumTypeUtilities<TEnum>.EnumMember)x.Value).Value) &&
					(!IsFlagEnum ||!((EnumTypeUtilities<TEnum>.EnumMember)x.Value).Value.Equals(GetZeroValue())));
		}

		public static TEnum GetZeroValue() => (TEnum) Convert.ChangeType(0, Enum.GetUnderlyingType(typeof (TEnum)));

		private static MethodInfo m_getValueString;
		
		public static string GetValueStringReflection(TEnum valueEntrySmartValue, out SdfIconType sdfIcon)
		{
			if (m_getValueString == null)
				m_getValueString =
					typeof(EnumSelector<TEnum>).GetMethod("GetValueString",
						BindingFlags.Static | BindingFlags.NonPublic);

			sdfIcon = SdfIconType.None;
			var args = new[] { (object)valueEntrySmartValue, sdfIcon };
			var result = (string)m_getValueString.Invoke(null, args);
			sdfIcon = (SdfIconType)args[1];
			return result;
		}
	}
}