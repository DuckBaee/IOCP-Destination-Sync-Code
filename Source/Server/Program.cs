using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

class IOCPServer
{
    private const int Port = 8080; // 서버 포트 번호 설정
    private const int BufferSize = 1024; // 버퍼 크기 설정
    private const int MaxClients = 250; // 최대 클라이언트 수 설정

    private Socket listenSocket; // 서버 소켓 객체
    private SemaphoreSlim semaphore; // 클라이언트 연결을 제한하는 세마포어
    private SocketAsyncEventArgsPool readWritePool; // SocketAsyncEventArgs 객체 풀
    private ConcurrentDictionary<Socket, SocketAsyncEventArgs> clients; // 연결된 클라이언트 목록
    private ConcurrentDictionary<string, Socket> clientIdMap; // PlayerID와 클라이언트 소켓을 매핑
    private ConcurrentDictionary<string, string> clientPositions; // PlayerID와 위치 정보를 매핑

    private int playerIdCounter; // PlayerID를 생성하기 위한 카운터

    public IOCPServer()
    {
        clientPositions = new ConcurrentDictionary<string, string>(); // 연결했을 때 다른 클라이언트들의 초기 위치를 알려주기 위한 딕셔너리 객체
        semaphore = new SemaphoreSlim(MaxClients, MaxClients); // 세마포어 초기화 (최대 클라이언트 수 설정)
        readWritePool = new SocketAsyncEventArgsPool(MaxClients, BufferSize, this); // SocketAsyncEventArgs 풀 초기화, 현재 IOCPServer 객체 전달
        clients = new ConcurrentDictionary<Socket, SocketAsyncEventArgs>(); // 연결된 클라이언트 목록 초기화
        clientIdMap = new ConcurrentDictionary<string, Socket>(); // PlayerID와 클라이언트 소켓 매핑 초기화
        playerIdCounter = 0; // PlayerID 카운터 초기화
    }

    public void Start()
    {
        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // 서버 소켓 생성
        listenSocket.Bind(new IPEndPoint(IPAddress.Any, Port)); // 서버 소켓을 지정된 포트에 바인딩
        listenSocket.Listen(100); // 서버 소켓을 리슨 모드로 설정 (최대 대기 연결 수 설정)

        for (int i = 0; i < MaxClients; i++) // 최대 클라이언트 수만큼 반복
        {
            var acceptEventArg = new SocketAsyncEventArgs(); // 새로운 SocketAsyncEventArgs 객체 생성
            acceptEventArg.Completed += AcceptCompleted; // 비동기 완료 이벤트 핸들러 등록
            if (!listenSocket.AcceptAsync(acceptEventArg)) // 비동기 클라이언트 연결 수락 시작
            {
                AcceptCompleted(this, acceptEventArg); // 비동기 작업이 완료된 경우 처리
            }
        }

        Console.WriteLine("Server started on port " + Port); // 서버 시작 메시지 출력
    }

    private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success) // 클라이언트 연결 수락 성공 시
        {
            semaphore.Wait(); // 세마포어 대기 (클라이언트 수 제한)
            var readEventArgs = readWritePool.Pop(); // SocketAsyncEventArgs 풀에서 객체 가져오기
            readEventArgs.UserToken = e.AcceptSocket; // 클라이언트 소켓 저장
            clients.TryAdd(e.AcceptSocket, readEventArgs); // 클라이언트를 목록에 추가

            // 고유한 PlayerID 생성
            string playerId = "ID" + (++playerIdCounter);
            clientIdMap.TryAdd(playerId, e.AcceptSocket); // PlayerID와 클라이언트 소켓 매핑

            Console.WriteLine("클라이언트 연결 완료: " + playerId);

            // 새로운 클라이언트에게 초기 위치 정보와 PlayerID 전송
            SendInitialPosition(e.AcceptSocket, "I:" + playerId + ",0,0,0");

            // 모든 클라이언트들의 현재 위치와 PlayerID를 새로운 클라이언트에게 전송.
            SendAllPositionsToClient(e.AcceptSocket);

            if (!e.AcceptSocket.ReceiveAsync(readEventArgs)) // 비동기 데이터 수신 시작
            {
                ProcessReceive(readEventArgs); // 비동기 작업이 완료된 경우 데이터 처리
            }
        }

        e.AcceptSocket = null; // 클라이언트 소켓 초기화
        if (!listenSocket.AcceptAsync(e)) // 다음 클라이언트 연결 수락 시작
        {
            AcceptCompleted(this, e); // 비동기 작업이 완료된 경우 처리
        }
    }

    public void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
    {
        ProcessReceive(e); // 데이터 수신 완료 시 데이터 처리
    }

    //클라이언트에 데이터를 보낼 때 처리할 로직들=============================================================================================

    private void ProcessReceive(SocketAsyncEventArgs e)
    {
        var socket = (Socket)e.UserToken; // 클라이언트 소켓 가져오기

        if (e.SocketError == SocketError.Success && e.BytesTransferred > 0) // 데이터 수신 성공 시
        {
            var receivedText = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred); // 수신된 데이터를 문자열로 변환
            Console.WriteLine("Received: " + receivedText); // 수신된 데이터 출력
            if (receivedText.StartsWith("P:"))
            {
                string[] parts = receivedText.Split(':');
                string playerId = parts[1].Split(',')[0];
                string position = parts[1].Substring(playerId.Length + 1);

                //클라이언트 위치 보관
                clientPositions[playerId] = position;

                // 수정된 BroadcastPosition 메서드 호출
                BroadcastPosition(socket, playerId, position);
            }
            else if (receivedText.StartsWith("Text:"))
            {
                string[] parts = receivedText.Split(':');
                string playerId = parts[1].Split(',')[0];
                string position = parts[1].Substring(playerId.Length + 1);
                BroadcastMessage(socket, playerId, position);
            }
            else if (receivedText.StartsWith("Disconnected:"))
            {
                string[] parts = receivedText.Split(':');
                string playerId = parts[1];
                BroadcastDisconnected(socket, playerId);
                CloseClientSocket2(playerId);
            }

            if (!socket.ReceiveAsync(e)) // 다음 데이터 수신 시작
            {
                ProcessReceive(e); // 비동기 작업이 완료된 경우 데이터 처리
            }
        }
        else
        {
            CloseClientSocket(e); // 오류 발생 시 클라이언트 소켓 닫기
        }
    }

    //=============================================================================================

    public void SendCompleted(object sender, SocketAsyncEventArgs e) // SendAsync 완료 시 호출
    {
        ProcessSend(e); // 데이터 전송 완료 시 데이터 처리
    }

    private void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success) // 데이터 전송 성공 시
        {
            readWritePool.Push(e); // 사용된 SocketAsyncEventArgs 객체를 풀에 반환
        }
        else
        {
            CloseClientSocket(e); // 오류 발생 시 클라이언트 소켓 닫기
        }
    }

    private void CloseClientSocket(SocketAsyncEventArgs e)
    {
        var socket = (Socket)e.UserToken; // 클라이언트 소켓 가져오기
        /*        socket.Close(); // 소켓 닫기
                semaphore.Release(); // 세마포어 해제 (클라이언트 수 제한 해제)*/
        readWritePool.Push(e); // 사용된 SocketAsyncEventArgs 객체를 풀에 반환
        clients.TryRemove(socket, out _); // 클라이언트 목록에서 제거

        // clientIdMap에서 해당 소켓을 가진 PlayerID 제거
        string playerIdToRemove = null;
        foreach (var kvp in clientIdMap)
        {
            if (kvp.Value == socket)
            {
                clientIdMap.TryRemove(kvp.Key, out _);
                playerIdToRemove = kvp.Key;
                break;
            }
        }
        if (playerIdToRemove != null)
        {
            clientPositions.TryRemove(playerIdToRemove, out _);
        }
    }
    private void CloseClientSocket2(string playerId)
    {
        if (clientIdMap.TryGetValue(playerId, out Socket socket))
        {
            var socketEventArgs = clients[socket]; // 클라이언트 목록에서 SocketAsyncEventArgs 가져오기
            CloseClientSocket(socketEventArgs);
        }
    }

    private void SendAllPositionsToClient(Socket clientSocket)
    {
        foreach (var kvp in clientPositions)
        {
            string playerID = kvp.Key;
            string position = kvp.Value;
            var buffer = Encoding.UTF8.GetBytes("I:" + playerID + "," + position + Environment.NewLine);
            var sendEventArgs = readWritePool.Pop(); //전송용 객체 가져오기
            sendEventArgs.SetBuffer(buffer, 0 ,buffer.Length);
            sendEventArgs.UserToken = clientSocket;
            Console.WriteLine(buffer);

            if (!clientSocket.SendAsync(sendEventArgs))
            {
                ProcessSend(sendEventArgs);
            }
        }
    }

    private void SendInitialPosition(Socket socket, string position)
    {
        var buffer = Encoding.UTF8.GetBytes(position + Environment.NewLine); // 위치 정보를 바이트 배열로 변환
        var sendEventArgs = readWritePool.Pop(); // 전송용 SocketAsyncEventArgs 객체 가져오기
        sendEventArgs.SetBuffer(buffer, 0, buffer.Length); // 전송 버퍼 설정
        sendEventArgs.UserToken = socket; // 클라이언트 소켓 저장
        bool asd = socket.SendAsync(sendEventArgs);
        Console.WriteLine(position);
        if (!asd) // 비동기 데이터 전송 시작
        {
            ProcessSend(sendEventArgs); // 비동기 작업이 완료된 경우 데이터 처리
        }
    }
    private void BroadcastMessage(Socket sender, string playerId, string message)
    {
        var buffer = Encoding.UTF8.GetBytes("Text:" + playerId + "," + message + Environment.NewLine); // 위치 정보를 바이트 배열로 변환
        foreach (var client in clients)
        {
            var sendEventArgs = readWritePool.Pop(); // 전송용 SocketAsyncEventArgs 객체 가져오기
            sendEventArgs.SetBuffer(buffer, 0, buffer.Length); // 전송 버퍼 설정
            sendEventArgs.UserToken = client.Key; // 클라이언트 소켓 저장

            if (!client.Key.SendAsync(sendEventArgs)) // 비동기 데이터 전송 시작
            {
                ProcessSend(sendEventArgs); // 비동기 작업이 완료된 경우 데이터 처리
            }
        }
    }

    private void BroadcastPosition(Socket sender, string playerId, string position)
    {
        var buffer = Encoding.UTF8.GetBytes("P:" + playerId + "," + position + Environment.NewLine); // 위치 정보를 바이트 배열로 변환
        foreach (var client in clients)
        {
            var sendEventArgs = readWritePool.Pop(); // 전송용 SocketAsyncEventArgs 객체 가져오기
            sendEventArgs.SetBuffer(buffer, 0, buffer.Length); // 전송 버퍼 설정
            sendEventArgs.UserToken = client.Key; // 클라이언트 소켓 저장

            if (!client.Key.SendAsync(sendEventArgs)) // 비동기 데이터 전송 시작
            {
                ProcessSend(sendEventArgs); // 비동기 작업이 완료된 경우 데이터 처리
            }
        }
    }
    // 플레이어가 끊겼을 때
    private void BroadcastDisconnected(Socket sender, string playerId)
    {
        var buffer = Encoding.UTF8.GetBytes("Disconnected:" + playerId + Environment.NewLine);
        foreach (var client in clients)
        {
            var sendEventArgs = readWritePool.Pop(); // 전송용 SocketAsyncEventArgs 객체 가져오기
            sendEventArgs.SetBuffer(buffer, 0, buffer.Length); // 전송 버퍼 설정
            sendEventArgs.UserToken = client.Key; // 클라이언트 소켓 저장

            if (!client.Key.SendAsync(sendEventArgs)) // 비동기 데이터 전송 시작
            {
                ProcessSend(sendEventArgs); // 비동기 작업이 완료된 경우 데이터 처리
            }
        }
    }

    static void Main(string[] args)
    {
        var server = new IOCPServer(); // 서버 객체 생성
        server.Start(); // 서버 시작
        Console.ReadLine(); // 사용자 입력 대기 (서버 종료 방지)
    }
}

class SocketAsyncEventArgsPool
{
    private readonly Queue<SocketAsyncEventArgs> pool; // SocketAsyncEventArgs 객체를 저장할 스택
    private readonly IOCPServer server; // IOCPServer 객체 참조

    public SocketAsyncEventArgsPool(int capacity, int bufferSize, IOCPServer server)
    {
        this.server = server; // IOCPServer 객체 저장
        pool = new Queue<SocketAsyncEventArgs>(capacity); // 지정된 용량으로 스택 초기화

        for (int i = 0; i < capacity; i++) // 지정된 용량만큼 반복
        {
            var eventArgs = new SocketAsyncEventArgs(); // 새로운 SocketAsyncEventArgs 객체 생성
            eventArgs.SetBuffer(new byte[bufferSize], 0, bufferSize); // 버퍼 설정

            // 비동기 완료 이벤트 핸들러 등록
            eventArgs.Completed += (sender, e) =>
            {
                var token = (Socket)e.UserToken; // 클라이언트 소켓 가져오기
                if (e.LastOperation == SocketAsyncOperation.Receive) // 마지막 작업이 데이터 수신일 경우
                {
                    server.ReceiveCompleted(sender, e); // 데이터 수신 완료 처리
                }
                else if (e.LastOperation == SocketAsyncOperation.Send) // 마지막 작업이 데이터 전송일 경우
                {
                    server.SendCompleted(sender, e); // 데이터 전송 완료 처리
                }
            };

            pool.Enqueue(eventArgs); // 생성된 SocketAsyncEventArgs 객체를 풀에 추가
        }
    }

    public SocketAsyncEventArgs Pop()
    {
        lock (pool) // 스레드 동기화를 위해 락 사용
        {
            return pool.Count > 0 ? pool.Dequeue() : null; // 풀에서 객체를 가져옴 (없으면 null 반환)
        }
    }

    public void Push(SocketAsyncEventArgs item)
    {
        if (item != null) // null 체크
        {
            lock (pool) // 스레드 동기화를 위해 락 사용
            {
                pool.Enqueue(item); // 사용된 객체를 풀에 반환
            }
        }
    }
}
