using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class NetworkManager : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private StreamWriter writer;
    private StreamReader reader;
    private int port = 8080;
    public string playerID; // 플레이어의 고유 ID를 저장할 변수
    //public GameObject playerPrefab;
    public GameObject otherPlayerPrefab;
    public GameObject playerPrefab; //신의 플레이어 객체
    Vector3 playerPosition;
    public float sendInterval = 0.5f;
    private Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>(); // 다른 클라이언트의 플레이어 객체 관리

    void Start()
    {
        ConnectToServer();
    }

    void Update()
    {
        playerPosition = gameObject.transform.position;
    }

    public async void ConnectToServer()
    {
        //CreatePlayer();
        // 서버에 연결 설정
        client = new TcpClient("127.0.0.1", port);
        stream = client.GetStream();
        writer = new StreamWriter(stream) { AutoFlush = true };
        reader = new StreamReader(stream);

        // 서버에서 할당된 PlayerID를 수신
        string initialMessage = await reader.ReadLineAsync();
        if (initialMessage.StartsWith("I:"))
        {
            var parts = initialMessage.Split(',');
            playerID = parts[0].Substring(2); // "I:" 이후의 PlayerID 추출
            //CreatePlayer();
            Debug.Log("Received PlayerID from server: " + playerID);
            // 서버에서 데이터를 비동기로 읽어오는 작업 시작
            StartCoroutine(SendPositionRoutine());
            StartListeningToServer();
        }
    }

    async void StartListeningToServer()
    {
        while (true)
        {
            string message = await reader.ReadLineAsync();
            if (message != null)
            {
                if (message.StartsWith("I:") && playerID != message.Substring(2).Split(",")[0])
                {
                    // 초기화 메시지를 받았을 때 새 플레이어를 생성
                    string playerId = message.Substring(2).Split(',')[0];
                    CreateOtherPlayer(playerId);
                }
                else if (message.StartsWith("P:"))
                {
                    // 위치 메시지를 받았을 때 플레이어의 위치를 업데이트
                    string[] parts = message.Substring(2).Split(',');
                    if (parts.Length == 4)
                    {
                        string playerId = parts[0];
                        float x = float.Parse(parts[1]);
                        float y = float.Parse(parts[2]);
                        float z = float.Parse(parts[3]);

                        Vector3 position = new Vector3(x, y, z);
                        if (playerID != playerId && !otherPlayers.ContainsKey(playerId))
                        {
                            CreateOtherPlayer(playerId);
                        }
                        else if (playerID != parts[0].Substring(2))
                            UpdatePlayerPosition(playerId, position);
                    }
                }
                else
                {

                }
            }
        }
    }

    private void CreateOtherPlayer(string playerId)
    {
        GameObject newPlayer = Instantiate(otherPlayerPrefab, otherPlayerPrefab.transform.position, otherPlayerPrefab.transform.rotation);
        otherPlayers[playerId] = newPlayer;
        Debug.Log("Created new other player: " + playerId);
    }
    
    IEnumerator SendPositionRoutine()
    {
        while (true)
        {
            SendPositionToServer();
            yield return new WaitForSeconds(sendInterval);
        }
    }
     async void SendPositionToServer()
     {
        while (true)
        {
            string message = $"P:{playerID},{playerPosition.x:F2},{playerPosition.y:F2},{playerPosition.z:F2}";
            if(message.StartsWith("P:"))
            await writer.WriteLineAsync(message);
            writer.Flush();
            return;
        }
     }

    private void UpdatePlayerPosition(string playerId, Vector3 position)
    {
        if (otherPlayers.ContainsKey(playerId))
        {
            otherPlayers[playerId].GetComponent<NavMeshAgent>().SetDestination(position);
        }
    }

    void OnDestroy()
    {
        writer.Close();
        reader.Close();
        stream.Close();
        client.Close();
    }
}
