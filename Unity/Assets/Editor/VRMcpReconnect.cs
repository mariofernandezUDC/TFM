// Editor-only helper. Does not change or save scenes or run the simulation.
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;

[InitializeOnLoad]
public static class VRMcpReconnect
{
    static VRMcpReconnect()
    {
        EditorApplication.delayCall += ConnectOnce;
    }

    [MenuItem("Tools/VR/Connect existing MCP server")]
    public static async void ConnectOnce()
    {
        // Reconnect on domain reload as the local server may have restarted.
        try
        {
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            SessionState.SetBool("TFM.VR.McpConnected", connected);
            Debug.Log("[TFM VR] MCP editor connection: " + connected);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[TFM VR] MCP connection: " + ex.Message);
        }
    }
}

