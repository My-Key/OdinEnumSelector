using System;

public class EnumSelectorAttribute : Attribute
{
	public string ValuesGetter;

	public EnumSelectorAttribute(string valuesGetter)
	{
		ValuesGetter = valuesGetter;
	}
}