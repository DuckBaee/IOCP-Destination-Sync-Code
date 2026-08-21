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
    public GameObject playerPrefab; //자신의 플레이어 객체
    public Dictionary<string, GameObject> Players = new Dictionary<string, GameObject>(); // 다른 클라이언트의 플레이어 객체 관리

    void Start()
    {
        ConnectToServer();
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            GetClickedPosition();
        }
    }

    public async void ConnectToServer()
    {
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
            Debug.Log("Received PlayerID from server: " + playerID);
            // 서버에서 데이터를 비동기로 읽어오는 작업 시작
            CreateMyPlayer(playerID);
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
                    string[] parts1 = message.Substring(2).Split(',');
                    if (parts1.Length == 4)
                    {
                        string playerId = parts1[0];
                        float x = float.Parse(parts1[1]);
                        float y = float.Parse(parts1[2]);
                        float z = float.Parse(parts1[3]);

                        Vector3 position = new Vector3(x, y, z);
                        CreateOtherPlayer(playerId, position);
                    }
                }
                else if (message.StartsWith("P:"))
                {
                    // 위치 메시지를 받았을 때 플레이어의 위치를 업데이트
                    string[] parts2 = message.Substring(2).Split(',');
                    if (parts2.Length == 4)
                    {
                        string playerId = parts2[0];
                        float x = float.Parse(parts2[1]);
                        float y = float.Parse(parts2[2]);
                        float z = float.Parse(parts2[3]);

                        Vector3 position = new Vector3(x, y, z);
                        if (playerID != playerId && !Players.ContainsKey(playerId))
                        {
                            CreateOtherPlayer(playerId, position);
                        }
                        else if (Players.ContainsKey(playerId))
                        {
                            UpdatePlayerPosition(playerId, position);
                        }
                    }
                }
            }
        }
    }

    void GetClickedPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // 마우스 클릭 위치에서 레이 생성
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit)) // 레이가 충돌했는지 확인
        {
            Debug.Log(hit.point);
            SendPositionToServer(hit.point);
        }
    }
    private void CreateMyPlayer(string playerId)
    {
        GameObject myPlayer = Instantiate(playerPrefab, playerPrefab.transform.position, playerPrefab.transform.rotation);
        myPlayer.GetComponent<Renderer>().material.color = Color.red;
        Players[playerId] =myPlayer;
        Debug.Log("Created my player: " + playerId);
        SendPositionToServer(playerPrefab.transform.position);
    }
    private void CreateOtherPlayer(string playerId, Vector3 position)
    {
        GameObject newPlayer = Instantiate(playerPrefab, position, playerPrefab.transform.rotation);
        newPlayer.GetComponent<NavMeshAgent>().SetDestination(position);
        newPlayer.GetComponent<Renderer>().material.color = Color.green;
        Players[playerId] = newPlayer;
        Debug.Log("Created new other player: " + playerId);
    }
    
     void SendPositionToServer(Vector3 movePosition)
     {
        while (true)
        {
            string message = $"P:{playerID},{movePosition.x:F2},{movePosition.y:F2},{movePosition.z:F2}";
            if(message.StartsWith("P:"))
            writer.WriteLine(message);
            writer.Flush();
            return;
        }
     }

    private void UpdatePlayerPosition(string playerId, Vector3 position)
    {
        if (Players.ContainsKey(playerId))
        {
            Players[playerId].GetComponent<NavMeshAgent>().SetDestination(position);
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
