namespace VBQOL;

public enum GameServerClientState
{
    Unknown,
    Client,
    Server
}

public static class Helper
{
    public static bool IsServer()
    {
        try
        {
            // Безопасная проверка ZNet.instance
            var znet = ZNet.instance;
            if (!znet) return false;
            
            return znet.IsServer();
        }
        catch
        {
            return false;
        }
    }

    public static bool IsClient()
    {
        try
        {
            var znet = ZNet.instance;
            if (!znet) return false;
            
            return !znet.IsServer();
        }
        catch
        {
            return false;
        }
    }

    public static GameServerClientState GetGameServerClientState()
    {
        try
        {
            var znet = ZNet.instance;
            if (!znet) return GameServerClientState.Unknown;
            
            return znet.IsServer() ? GameServerClientState.Server : GameServerClientState.Client;
        }
        catch
        {
            return GameServerClientState.Unknown;
        }
    }
    
    public static bool AreCheatsEnabled(Terminal terminal)
    {
        try
        {
            if (!Console.IsVisible()) return false;

            if (!terminal) return false;

            bool cheatEnabled = Traverse.Create(terminal).Field<bool>("m_cheat").Value;
            return cheatEnabled;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Ошибка проверки чит-команд: {ex.Message}");
            return false;
        }
    }
}