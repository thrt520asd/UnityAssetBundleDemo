using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor ;
using System.Reflection;

public static class ExtensionClass {
	
	private static PropertyInfo inspectorMode = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
	public static long GetFileID(this Object target)
	{
		SerializedObject serializedObject = new SerializedObject(target);
		inspectorMode.SetValue(serializedObject, InspectorMode.Debug, null);
		SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
		return localIdProp.longValue;
	}
}
