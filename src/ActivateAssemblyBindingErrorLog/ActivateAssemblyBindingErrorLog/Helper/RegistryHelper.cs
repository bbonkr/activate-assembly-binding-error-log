using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ActivateAssemblyBindingErrorLog.Helper
{
    /// <summary>
    /// Registry Helper class
    /// </summary>
    public class RegistryHelper
    {
        /// <summary>
        /// verify that the registry path exists.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool Exists(string path)
        {
            RegistryKey sub = GetRegistryKey(path);

            return (sub != null);
        }

        /// <summary>
        /// Get a sub key through registry path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="writable"></param>
        /// <param name="createSubKeyWhenDoesNotExist"></param>
        /// <returns></returns>
        public static RegistryKey GetRegistryKey(string path = "", bool writable = false, bool createSubKeyWhenDoesNotExist = false)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"The registry path does not allow null or empty string.", nameof(path));
            }

            bool exists = false;
            RegistryKey root = null;
            RegistryKey sub = null;
            RegistryKey foundKey = null;

            /*
             * x64 OS:  Target key is not 'HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Fusion'
             */
            RegistryView registryView = RegistryView.Registry64;
            if (Environment.Is64BitOperatingSystem)
            {
                registryView = RegistryView.Registry32;
            }

            string subKey = String.Empty;
            List<string> keys = new List<string>();
            if (path.Contains("\\"))
            {
                keys = path.Split('\\').ToList();
            }
            else
            {
                keys.Add(path);
            }

            if (keys.Count > 0)
            {
                switch (keys[0].ToUpper())
                {
                    case "HKLM":
                    case "HKEY_LOCAL_MACHINE":
                        //root = Registry.LocalMachine;
                        root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
                        break;
                    case "HKEY_CLASSES_ROOT":
                        //root = Registry.ClassesRoot;
                        root = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, registryView);
                        break;
                    case "HKEY_CURRENT_USER":
                        //root = Registry.CurrentUser;
                        root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, registryView);
                        break;
                    case "HKEY_USERS":
                        //root = Registry.Users;
                        root = RegistryKey.OpenBaseKey(RegistryHive.Users, registryView);
                        break;
                    case "HKEY_CURRENT_CONFIG":
                        //root = Registry.CurrentConfig;
                        root = RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, registryView);
                        break;
                }

                if (root == null)
                {
                    throw new ApplicationException($"This path is Invalid registry path. [{keys[0].ToUpper()}]");
                }

                if (root != null && keys.Count > 1)
                {
                    for (int i = 1, len = keys.Count; i < len; i++)
                    {
                        subKey = keys[i];
                        try
                        {
                            foundKey = (sub ?? root).OpenSubKey(subKey, writable);
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException($"Could not explore a sub key; [{subKey}].", ex);
                        }
                        exists = (foundKey != null);

                        if (!exists && writable && createSubKeyWhenDoesNotExist)
                        {
                            // add sub key when does not exists
                            try
                            {
                                foundKey = (sub ?? root).CreateSubKey(subKey);
                            }
                            catch (Exception ex)
                            {
                                throw new ApplicationException($"Could not create a sub key; [{subKey}]", ex);
                            }

                            exists = (foundKey != null);
                        }

                        if (!exists) { break; }
                        sub = foundKey;
                    }
                }
            }

            return sub;
        }

        /// <summary>
        /// Get value list through registry path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IEnumerable<RegistryValue> GetValues(string path = "", string name = "")
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"The registry path does not allow null or empty string.", nameof(path));
            }

            List<string> names = new List<string>();
            var subKey = GetRegistryKey(path);
            if (String.IsNullOrEmpty(name))
            {
                names = subKey.GetValueNames().ToList();
            }
            else
            {
                names.Add(name);
            }

            foreach (var n in names)
            {
                yield return new RegistryValue
                {
                    Name = n,
                    Value = subKey.GetValue(n),
                    ValueKind = subKey.GetValueKind(n)
                };
            }
        }

        /// <summary>
        /// Add value on registry path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="valueKind"></param>
        /// <param name="commit"></param>
        public static void AddValue(string path, string name, object value, RegistryValueKind valueKind, bool commit)
        {
            RegistryKey destKey = GetRegistryKey(path, true, true);
            try
            {
                destKey.SetValue(name, value, valueKind);

                if (commit)
                {
                    destKey.Flush();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Could not set value; [{name}]", ex);
            }
        }

        /// <summary>
        /// Add value on registry path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <param name="commit"></param>
        public static void AddValue(string path, RegistryValue value, bool commit)
        {
            AddValue(path, value.Name, value.Value, value.ValueKind, commit);
        }

        /// <summary>
        /// Add values on registry path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="values"></param>
        public static void AddValues(string path, params RegistryValue[] values)
        {
            if (values != null && values.Length > 0)
            {
                foreach (var value in values)
                {
                    AddValue(path, value, false);
                }

                RegistryKey destKey = GetRegistryKey(path, true, true);
                destKey.Flush();
            }
        }

        /// <summary>
        /// Remove value on registry path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="valueNames"></param>
        /// <param name="commit"></param>
        public static void RemoveValue(string path, string valueNames, bool commit)
        {
            RegistryKey destKey = GetRegistryKey(path, true, true);
            try
            {
                destKey.DeleteValue(valueNames, true);

                if (commit)
                {
                    destKey.Flush();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Could not delete value; [{valueNames}]", ex);
            }
        }

        /// <summary>
        /// Remove values on registry path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="valueNames"></param>
        public static void RemoveValues(string path, params string[] valueNames)
        {
            if (valueNames != null && valueNames.Length > 0)
            {
                foreach (var name in valueNames)
                {
                    RemoveValue(path, name, false);
                }

                RegistryKey destKey = GetRegistryKey(path, true, true);
                destKey.Flush();
            }
        }

        /// <summary>
        /// Remove values on registry path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="values"></param>
        public static void RemoveValues(string path, params RegistryValue[] values)
        {
            RemoveValues(path, values.Select(v => v.Name).ToArray());
        }
    }

    /// <summary>
    /// Registry value data container
    /// </summary>
    public class RegistryValue
    {
        public RegistryValue()
        {

        }

        public RegistryValue(string name, object value, RegistryValueKind valueKind)
        {
            Name = name;
            Value = value;
            ValueKind = valueKind;
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Kind of value
        /// </summary>
        public RegistryValueKind ValueKind { get; set; }
    }
}
