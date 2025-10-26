namespace VBBossChatter
{
    [Serializable]
    public class BossMessageConfig
    {
        public List<BossConfigEntry> BossPrefabs { get; set; } = new List<BossConfigEntry>();
        
        // Настройки по умолчанию для новых боссов
        public BossConfigEntry DefaultSettings { get; set; } = new BossConfigEntry
        {
            Enabled = true,
            ConfigFile = "{name}_messages.json",
            DespawnMessagesChance = 100,
            LostMessagesChance = 100,
            KillMessagesChance = 100,
            TauntMessagesChance = 100,
            RareTauntMessagesChance = 100,
            DamageTauntMessagesChance = 100,
            BlockTauntMessagesChance = 100,
            HealTauntMessagesChance = 100,
            AggroTauntMessagesChance = 100,
            FoodTauntMessagesChance = 100,
            PotionTauntMessagesChance = 100,
            BerryTauntMessagesChance = 100,
            MushroomTauntMessagesChance = 100
        };
    }

    [Serializable]
    public class BossConfigEntry
    {
        public string PrefabName { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public string ConfigFile { get; set; } = "";
        
        // Шансы показа сообщений для каждого типа (0-100)
        public int DespawnMessagesChance { get; set; } = 100;
        public int LostMessagesChance { get; set; } = 100;
        public int KillMessagesChance { get; set; } = 100;
        public int TauntMessagesChance { get; set; } = 100;
        public int RareTauntMessagesChance { get; set; } = 100;
        public int DamageTauntMessagesChance { get; set; } = 100;
        public int BlockTauntMessagesChance { get; set; } = 100;
        public int HealTauntMessagesChance { get; set; } = 100;
        public int AggroTauntMessagesChance { get; set; } = 100;
        public int FoodTauntMessagesChance { get; set; } = 100;
        public int PotionTauntMessagesChance { get; set; } = 100;
        public int BerryTauntMessagesChance { get; set; } = 100;
        public int MushroomTauntMessagesChance { get; set; } = 100;
        
        // Получить шанс для типа сообщения
        public int GetChanceForMessageType(string messageType)
        {
            return messageType.ToLower() switch
            {
                "despawn" => DespawnMessagesChance,
                "lost" => LostMessagesChance,
                "kill" => KillMessagesChance,
                "taunt" => TauntMessagesChance,
                "raretaunt" => RareTauntMessagesChance,
                "damagetaunt" => DamageTauntMessagesChance,
                "blocktaunt" => BlockTauntMessagesChance,
                "healtaunt" => HealTauntMessagesChance,
                "aggrotaunt" => AggroTauntMessagesChance,
                "foodtaunt" => FoodTauntMessagesChance,
                "potiontaunt" => PotionTauntMessagesChance,
                "berrytaunt" => BerryTauntMessagesChance,
                "mushroomtaunt" => MushroomTauntMessagesChance,
                _ => 100
            };
        }
    }

    [Serializable]
    public class BossMessages
    {
        public string[] DespawnMessages { get; set; } = new string[]
        {
            "Ты не достоин моей ярости. Я исчезаю.",
            "Скука… Вернусь, когда найдётся смелый воин.",
            "Трусость твоя спасла тебя лишь на время.",
            "Я ухожу в тьму, но мы ещё встретимся.",
            "Ты сбежал? Тогда я заберу у тебя надежду."
        };

        public string[] LostMessages { get; set; } = new string[]
        {
            "Не смей отворачиваться от меня!",
            "Вернись и сразись, если не трус!",
            "Ты не убежишь от своей гибели!",
            "Я ещё не насытился твоим страхом!",
            "Смертный, твой бег лишь продлевает муки!"
        };

        public string[] KillMessages { get; set; } = new string[]
        {
            "Вот так умирают слабые!",
            "Ещё один смертный пал предо мной!",
            "Ха-ха! Твоя жизнь окончена!",
            "Ты был лишь игрушкой для моей силы!",
            "Смерть твоя — моя забава!"
        };

        public string[] TauntMessages { get; set; } = new string[]
        {
            "Снова ты? Ты ничему не учишься!",
            "Я уже привык видеть твою смерть!",
            "Ты вернулся за новым поражением?",
            "Ха-ха! Ты снова пришёл умирать?",
            "Я начинаю скучать без твоих криков!"
        };

        public string[] RareTauntMessages { get; set; } = new string[]
        {
            "Ты — мой вечный источник забавы!",
            "Я храню твои крики в своей памяти!",
            "Смерть твоя стала для меня привычкой!",
            "Ты словно тень, всегда возвращаешься ко мне!",
            "Я начинаю думать, что ты любишь умирать от моей руки!"
        };

        public string[] DamageTauntMessages { get; set; } = new string[]
        {
            "Жало комара! Ты лишь разозлил меня!",
            "Это всё, на что ты способен? Смешно!",
            "Ты оставил царапину! Гордись ею перед смертью!",
            "Мои раны лишь усиливают мою ярость!",
            "Ты думаешь, что можешь ранить меня? Наивный смертный!"
        };

        public string[] BlockTauntMessages { get; set; } = new string[]
        {
            "Ты думаешь, этот щит спасёт тебя?",
            "Защищайся, слабак! Всё равно сломаю твою защиту!",
            "Ха! Твой щит — как бумага перед моей силой!",
            "Продолжай прятаться! Это лишь продлит твои мучения!",
            "Твой блок смешон! Я разнесу его в щепки!"
        };

        public string[] HealTauntMessages { get; set; } = new string[]
        {
            "Лечи свои раны! Всё равно умрёшь!",
            "Трава не спасёт тебя от меня!",
            "Продолжай жевать свои корешки!",
            "Исцеляйся! Мне нравится растягивать твои мучения!",
            "Каждая твоя зелье лишь отдаляет неизбежное!"
        };

        public string[] AggroTauntMessages { get; set; } = new string[]
        {
            "Приготовься к смерти, {player}!",
            "А, {player}! Как раз вовремя!",
            "Смерть ждёт тебя, {player}!",
            "Твои мучения начинаются, {player}!",
            "Я разорву тебя на части, {player}!"
        };

        public string[] FoodTauntMessages { get; set; } = new string[]
        {
            "Жуй свою еду, слабак!",
            "Набивай брюхо перед смертью!",
            "Твоя последняя трапеза, смертный!",
            "Поешь перед тем как я съем тебя!",
            "Наслаждайся едой - это твой последний ужин!"
        };

        public string[] PotionTauntMessages { get; set; } = new string[]
        {
            "Зелье не спасёт тебя от меня!",
            "Пей свои снадобья, трус!",
            "Твои зелья бесполезны против моей мощи!",
            "Каждая капля зелья - напрасная трата!",
            "Исцеляйся! Мне нравится растягивать твои мучения!"
        };

        public string[] BerryTauntMessages { get; set; } = new string[]
        {
            "Хрусти ягодами перед смертью!",
            "Твои ягоды не сравнятся с моей силой!",
            "Поешь ягод перед гибелью!",
            "Ягоды не спасут тебя от меня!"
        };

        public string[] MushroomTauntMessages { get; set; } = new string[]
        {
            "Грибы не сделают тебя сильнее!",
            "Жуй свои грибы, слабак!",
            "Твои грибы бесполезны против меня!"
        };

        public HashSet<string> PotionPrefabs { get; set; } = new HashSet<string>
        {
            "MeadHealthMinor",
            "MeadHealthMedium",
            "MeadHealthMajor"
        };

        public HashSet<string> FoodPrefabs { get; set; } = new HashSet<string>
        {
            "BloodPudding",
            "Sausages",
            "TurnipStew",
            "CarrotSoup",
            "Muckshake",
            "SerpentStew",
            "Bread",
            "LoxPie",
            "FishWraps",
            "Eyescream"
        };

        public HashSet<string> BerryPrefabs { get; set; } = new HashSet<string>
        {
            "Raspberry",
            "Blueberries",
            "Cloudberry"
        };

        public HashSet<string> MushroomPrefabs { get; set; } = new HashSet<string>
        {
            "Mushroom",
            "MushroomYellow"
        };
    }
}