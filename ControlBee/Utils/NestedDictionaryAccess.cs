namespace ControlBee.Utils;

public class NestedDictionaryAccess(object nestedDictionary)
{
    public NestedDictionaryAccess this[string key]
    {
        get
        {
            var obj = ((Dictionary<object, object>)nestedDictionary)[key];
            return new NestedDictionaryAccess(obj);
        }
    }

    public object Value => nestedDictionary;
}
