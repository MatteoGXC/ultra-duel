using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MDPro3.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDPro3
{
    [Serializable]
    public class SettingData
    {
        public int[] FinalAttackBlueEyes;
        public int[] FinalAttackDarkM;
        public int[] FinalAttackRedEyes;
        public int[] FinalAttackObelisk;
        public int[] FinalAttackRa;
        public int[] FinalAttackSlifer;

        public string PrereleasePackUrl;
        public string PrereleasePackVersionUrl;
        public string PrereleasePackUrl_DE;
        public string PrereleasePackVersionUrl_DE;
        public string PrereleasePackUrl_EN;
        public string PrereleasePackVersionUrl_EN;
        public string PrereleasePackUrl_ES;
        public string PrereleasePackVersionUrl_ES;
        public string PrereleasePackUrl_FR;
        public string PrereleasePackVersionUrl_FR;
        public string PrereleasePackUrl_IT;
        public string PrereleasePackVersionUrl_IT;
        public string PrereleasePackUrl_JP;
        public string PrereleasePackVersionUrl_JP;
        public string PrereleasePackUrl_KR;
        public string PrereleasePackVersionUrl_KR;
        public string PrereleasePackUrl_PT;
        public string PrereleasePackVersionUrl_PT;
        public string PrereleasePackUrl_TW;
        public string PrereleasePackVersionUrl_TW;

        public bool CardRenderPassword;
        public int[] SavedCardSize;
        public string SavedCardFormat;
        public bool BatchMove;
        public string DiySymbol;

        public SettingData()
        {
            FinalAttackBlueEyes = new int[]
            {
                89631139,   //青眼白龙
                53347303,   //青眼光龙
                22804410,   //渊眼白龙
                38517737,   //青眼亚白龙
                30576089,   //青眼喷气龙
                9433350,    //罪 青眼白龙
                53183600,   //青眼卡通龙
                23995346,   //青眼究极龙
                43228023,   //青眼究极亚龙
                56532353,   //真青眼究极龙
                2129638,    //青眼双爆裂龙
                11443677    //青眼暴君龙
            };

            FinalAttackDarkM = new int[]
            {
                46986414,   //黑魔术师
                92377303,   //黑衣大贤者
                342673,       //黑色魔术师-黑魔术师
                21296502,   //卡通黑魔术师
                29436665,   //黑魔导执行官
                35191415,   //黑幻想之魔术师
                38033121,   //黑魔术少女
                90960358,   //卡通黑魔术少女
                50237654    //超魔导师-黑魔术师徒
            };

            FinalAttackRedEyes = new int[]
            {
                74677422,   //真红眼黑龙
                96561011,   //真红眼暗龙
                64335804,   //真红眼黑钢龙
                18491580,   //真红眼亚黑龙
                55343236,   //罪 真红眼黑龙
                6556909,     //真红之魂
            };

            FinalAttackObelisk = new int[]
            {
                10000000   //巨神兵
            };

            FinalAttackRa = new int[]
            {
                10000010,   //翼神龙
                10000080,   //蛋
                10000090,   //不死鸟
            };

            FinalAttackSlifer = new int[]
            {
                10000020   //天空龙
            };

            PrereleasePackUrl = "https://cdn02.moecube.com:444/ygopro-super-pre/archive/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl = "https://cdn02.moecube.com:444/ygopro-super-pre/data/version.txt";

            PrereleasePackUrl_DE = "https://github.com/ElderLich/TransSuperpre/raw/main/DE/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_DE = "https://github.com/ElderLich/TransSuperpre/raw/main/DE/version.txt";
            PrereleasePackUrl_EN = "https://github.com/ElderLich/TransSuperpre/raw/main/EN/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_EN = "https://github.com/ElderLich/TransSuperpre/raw/main/EN/version.txt";
            PrereleasePackUrl_ES = "https://github.com/ElderLich/TransSuperpre/raw/main/ES/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_ES = "https://github.com/ElderLich/TransSuperpre/raw/main/ES/version.txt";
            PrereleasePackUrl_FR = "https://github.com/ElderLich/TransSuperpre/raw/main/FR/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_FR = "https://github.com/ElderLich/TransSuperpre/raw/main/FR/version.txt";
            PrereleasePackUrl_IT = "https://github.com/ElderLich/TransSuperpre/raw/main/IT/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_IT = "https://github.com/ElderLich/TransSuperpre/raw/main/IT/version.txt";
            PrereleasePackUrl_JP = "https://github.com/ElderLich/TransSuperpre/raw/main/JP/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_JP = "https://github.com/ElderLich/TransSuperpre/raw/main/JP/version.txt";
            PrereleasePackUrl_KR = "https://github.com/ElderLich/TransSuperpre/raw/main/KR/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_KR = "https://github.com/ElderLich/TransSuperpre/raw/main/KR/version.txt";
            PrereleasePackUrl_PT = "https://github.com/ElderLich/TransSuperpre/raw/main/PT/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_PT = "https://github.com/ElderLich/TransSuperpre/raw/main/PT/version.txt";
            PrereleasePackUrl_TW = "https://github.com/ElderLich/TransSuperpre/raw/main/ZH-TW/ygopro-super-pre.ypk";
            PrereleasePackVersionUrl_TW = "https://github.com/ElderLich/TransSuperpre/raw/main/ZH-TW/version.txt";

            CardRenderPassword = true;
            SavedCardSize = new int[] { 704, 1024 };
            SavedCardFormat = Program.EXPANSION_JPG;
            BatchMove = true;
            DiySymbol = "DIY by";
        }
    }

    public static class Settings
    {
        private const string PATH_JSON = Program.PATH_DATA + "Settings.json";

        private static SettingData _data;
        public static SettingData Data
        {
            get
            {
                if(_data == null)
                    Initialize();
                return _data;
            }
        }

        public static void Initialize()
        {
            if (!File.Exists(PATH_JSON))
            {
                _data = new SettingData();
                SaveSettings(_data);
                return;
            }

            var json = File.ReadAllText(PATH_JSON);
            try
            {
                _data = EnsureDefaultValues(json);
            }
            catch(JsonReaderException ex)
            {
                MessageManager.Cast("Failed to parse Settings.json: " + ex.Message);
                _data = new SettingData();
            }
        }

        public static string GetPrereleasePackUrl()
        {
            return Language.GetConfig() switch
            {
                Language.German => Data.PrereleasePackUrl_DE,
                Language.English => Data.PrereleasePackUrl_EN,
                Language.Spanish => Data.PrereleasePackUrl_ES,
                Language.French => Data.PrereleasePackUrl_FR,
                Language.Italian => Data.PrereleasePackUrl_IT,
                Language.Japanese => Data.PrereleasePackUrl_JP,
                Language.Korean => Data.PrereleasePackUrl_KR,
                Language.Portuguese => Data.PrereleasePackUrl_PT,
                Language.TraditionalChinese => Data.PrereleasePackUrl_TW,
                _ => Data.PrereleasePackUrl
            };
        }

        public static string GetPrereleasePackVersionUrl()
        {
            return Language.GetConfig() switch
            {
                Language.German => Data.PrereleasePackVersionUrl_DE,
                Language.English => Data.PrereleasePackVersionUrl_EN,
                Language.Spanish => Data.PrereleasePackVersionUrl_ES,
                Language.French => Data.PrereleasePackVersionUrl_FR,
                Language.Italian => Data.PrereleasePackVersionUrl_IT,
                Language.Japanese => Data.PrereleasePackVersionUrl_JP,
                Language.Korean => Data.PrereleasePackVersionUrl_KR,
                Language.Portuguese => Data.PrereleasePackVersionUrl_PT,
                Language.TraditionalChinese => Data.PrereleasePackVersionUrl_TW,
                _ => Data.PrereleasePackVersionUrl
            };
        }

        private static void SaveSettings(SettingData data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(PATH_JSON, json);
        }

        private static SettingData EnsureDefaultValues(string json)
        {
            var data = JsonConvert.DeserializeObject<SettingData>(json);

            bool needOverwrite = false;
            var jObject = JObject.Parse(json);
            var missingFields = GetMissingFields<SettingData>(jObject);
            if (missingFields.Count > 0)
                needOverwrite = true;

            if (needOverwrite)
                SaveSettings(data);

            return data;
        }

        private static List<string> GetMissingFields<T>(JObject jObject)
        {
            var missingFields = new List<string>();
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
                if (!jObject.ContainsKey(field.Name))
                    missingFields.Add(field.Name);
            return missingFields;
        }
    }
}
