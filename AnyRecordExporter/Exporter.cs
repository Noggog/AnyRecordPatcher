﻿using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using YamlDotNet.Serialization;

namespace AnyRecordExporter;
using AnyRecordData.DataTypes;

public static class Exporter
{
    private static string _pluginName = "Unofficial Skyrim Special Edition Patch.esp";
    private static string _parentFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private static string _patchFolder = "";

    private static bool _saveBookText = true;
    
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();
    
    public static void Main(string[] args)
    {
        foreach (string arg in args)
        {
            if (arg.ToLower().StartsWith("plugin="))
            {
                _pluginName = arg[7..];
                continue;
            }

            if (arg.ToLower().StartsWith("path="))
            {
                _parentFolder = Path.GetFullPath(arg[5..]);
                continue;
            }

            if (arg.ToLower().StartsWith("savebooktext="))
            {
                _saveBookText = arg.ToLower().Equals("savebooktext=true");
            }
        }

        Regex regex = new Regex(@"\.es(p|m)", RegexOptions.IgnoreCase);
        _patchFolder = _parentFolder + "\\" + regex.Replace(_pluginName, "");
        
        using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
        GetChanges(env);
    }
    
    public static void GetChanges(IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env)
    {
        if (!CheckForMod(env, _pluginName))
        {
            Console.WriteLine($"ERR: Mod: {_pluginName} not found. Exiting...");
            return;
        }
        
        Console.WriteLine($"INFO: Creating config patch for mod: {_pluginName}");
        
        if (!_saveBookText)
            Console.WriteLine("INFO: Saving of Book Text to config file disabled by user");

        SaveChanges<IAmmunitionGetter, DataAmmo>( 
            GetModifiedRecords<IAmmunitionGetter>(env));
        SaveChanges<IArmorGetter, DataArmor>(
            GetModifiedRecords<IArmorGetter>(env));
        SaveChanges<IBookGetter, DataBook>(
            GetModifiedRecords<IBookGetter>(env));
        SaveChanges<ICellGetter, DataCell>(
            GetModifiedRecords<ICellGetter>(env));
        SaveChanges<IIngestibleGetter, DataIngestible>(
            GetModifiedRecords<IIngestibleGetter>(env));
        SaveChanges<IIngredientGetter, DataIngredient>(
            GetModifiedRecords<IIngredientGetter>(env));
        SaveChanges<ILightGetter, DataLight>(
            GetModifiedRecords<ILightGetter>(env));
        SaveChanges<IMiscItemGetter, DataMisc>(
            GetModifiedRecords<IMiscItemGetter>(env));
        SaveChanges<IPerkGetter, DataPerk>(
            GetModifiedRecords<IPerkGetter>(env));
        SaveChanges<IScrollGetter, DataScroll>(
            GetModifiedRecords<IScrollGetter>(env));
        SaveChanges<ISoulGemGetter, DataSoulGem>(
            GetModifiedRecords<ISoulGemGetter>(env));
        SaveChanges<IShoutGetter, DataShout>(
            GetModifiedRecords<IShoutGetter>(env));
        SaveChanges<ISpellGetter, DataSpell>(
            GetModifiedRecords<ISpellGetter>(env));
        SaveChanges<IWeaponGetter, DataWeapon>(
            GetModifiedRecords<IWeaponGetter>(env));
    }
    
    private static List<(T, T)> GetModifiedRecords<T>(IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env)
        where T : ISkyrimMajorRecordGetter
    {
        Dictionary<FormKey, T> newRefs = new();
        Dictionary<FormKey, T> oldRefs = new();
        List<(T, T)> compares = new();

        // Get override version of record we care about
        foreach (var mod in env.LoadOrder.ListedOrder.OnlyEnabled())
        {
            if (!mod.FileName.Equals(_pluginName) || mod.Mod is null)
                continue;
            
            foreach (T newRef in mod.Mod.EnumerateMajorRecords(typeof(T)))
            {
                newRefs.Add(newRef.FormKey, newRef);
            }
        }

        // Get previous definition (or override?)
        foreach (var mod in env.LoadOrder.ListedOrder.OnlyEnabled())
        {
            if (mod.FileName.Equals(_pluginName))
                break;

            if (mod.Mod is null)
                continue;

            foreach (T oldRef in mod.Mod.EnumerateMajorRecords(typeof(T)))
            {
                if (newRefs.ContainsKey(oldRef.FormKey))
                {
                    oldRefs[oldRef.FormKey] = oldRef;
                }
            }
        }

        foreach (T over in newRefs.Values)
        {
            if (oldRefs.TryGetValue(over.FormKey, out T? def))
            {
                compares.Add((over, def));
            }
        }

        return compares;
    }

    // Go through the list of comparable records and serialize the modifications to disk.
    private static void SaveChanges<T, TE>(List<(T, T)> compares)
        where T : ISkyrimMajorRecordGetter
        where TE : DataBaseItem, new()
    {
        if (compares.Count < 0)
            return;

        string type = "Default";
        
        List<string> strings = new();
        bool modifiedCategory = false;
        foreach ((T newRef, T oldRef) in compares)
        {
            // Reset shared object
            TE data = new()
            {
                Id = newRef.FormKey.ToString()
            };
            type = data.PatchFileName;
            
            switch (newRef, oldRef)
            {
                case (IAmmunitionGetter, IAmmunitionGetter) x :
                    data.GetData((IAmmunitionGetter)x.newRef, (IAmmunitionGetter)x.oldRef);
                    break;
                case (IArmorGetter, IArmorGetter) x :
                    data.GetData((IArmorGetter)x.newRef, (IArmorGetter)x.oldRef);
                    break;
                case (IBookGetter, IBookGetter) x :
                    data.GetData((IBookGetter)x.newRef, (IBookGetter)x.oldRef);
                    break;
                case (ICellGetter, ICellGetter) x :
                    data.GetData((ICellGetter)x.newRef, (ICellGetter)x.oldRef);
                    break;
                case (IIngestibleGetter, IIngestibleGetter) x :
                    data.GetData((IIngestibleGetter)x.newRef, (IIngestibleGetter)x.oldRef);
                    break;
                case (IIngredientGetter, IIngredientGetter) x :
                    data.GetData((IIngredientGetter)x.newRef, (IIngredientGetter)x.oldRef);
                    break;
                case (ILightGetter, ILightGetter) x :
                    data.GetData((ILightGetter)x.newRef, (ILightGetter)x.oldRef);
                    break;
                case (IMiscItemGetter, IMiscItemGetter) x :
                    data.GetData((IMiscItemGetter)x.newRef, (IMiscItemGetter)x.oldRef);
                    break;
                case (IPerkGetter, IPerkGetter) x :
                    data.GetData((IPerkGetter)x.newRef, (IPerkGetter)x.oldRef);
                    break;
                case (IScrollGetter, IScrollGetter) x :
                    data.GetData((IScrollGetter)x.newRef, (IScrollGetter)x.oldRef);
                    break;
                case (ISoulGemGetter, ISoulGemGetter) x :
                    data.GetData((ISoulGemGetter)x.newRef, (ISoulGemGetter)x.oldRef);
                    break;
                case (IShoutGetter, IShoutGetter) x :
                    data.GetData((IShoutGetter)x.newRef, (IShoutGetter)x.oldRef);
                    break;
                case (ISpellGetter, ISpellGetter) x :
                    data.GetData((ISpellGetter)x.newRef, (ISpellGetter)x.oldRef);
                    break;
                case (IWeaponGetter, IWeaponGetter) x :
                    data.GetData((IWeaponGetter)x.newRef, (IWeaponGetter)x.oldRef);
                    break;
            }

            if (data is DataBook dataBook)
            {
                if (!_saveBookText)
                    dataBook.Text = null;
            }
            
            if (!data.IsModified())
                continue;
            
            modifiedCategory = true;

            string cereal = Serializer.Serialize(data);
            if (cereal.Trim().Length > 0)
            {
                strings.Add(cereal.Replace("Deleted: true", ": null"));
            }
        }

        // Write text to file, if at least one thing is modified
        if (!modifiedCategory) return;
        
        if (!Directory.Exists(_patchFolder))
            Directory.CreateDirectory(_patchFolder);
            
        File.WriteAllText($@"{_patchFolder}\{type}.yaml", string.Join("---\r\n", strings));
    }

    private static bool CheckForMod(IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env, string modName)
    {
        return env.LoadOrder.ListedOrder.Any(mod => mod.FileName.Equals(modName));
    }
}