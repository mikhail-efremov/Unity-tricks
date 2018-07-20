using UnityEngine;

namespace Editor.SelectItemViaDropdown
{
  public class DropdownMonobehaviour : MonoBehaviour
  {
    [SerializeField, HideInInspector]
    private string _methodName = "";
    
    [SerializeField, HideInInspector]
    private string _typeName = "";
  }
}
