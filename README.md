# P2P Deterministic Lockstep

![Demo GIF](./ReadmeAssets/lockstep.gif)

Demo with an input delay of 6 ticks, artificial latency of 25 ms (50ms ping) and artificial packet loss of 5%. Synchronisation is achieved. Network conditions weren't turbulent enough to cause freezing/locking.

Used C#, Unity and [DarkRift 2](https://www.darkriftnetworking.com/).

Based on [Divekick](https://store.steampowered.com/app/244730/Divekick/), a minimalistic fighting game.

This project is a technical exercise and isn't production-ready.


## Lockstep theory

### Advantages, disadvantages and applications

- [+] True synchronisation
- [+] Simple to implement
- [-] Input delay makes for unresponsive gameplay
- [-] Stuttering in turbulent network conditions makes for a terrible experience
- [-] Deterministic input required

Lockstep is used in some RTS games and older fighting games. For the most part, its lack of responsiveness makes it undesirable.

However, its principles pave a good foundation for further augmentations.

### Lockstep

Lockstep is perhaps the most simplistic viable networking model.

In essence, we play the game at a delay of D ticks. The player's input is collected and sent over the network instantly, but only rendered after D ticks.

We create this delay in the hope that we will receive the other player's input in time for rendering.

This is analogous to buffering a YouTube video. We don't stream data the moment we get it. Instead, we inject a delay, which builds up a buffer that ensures data is always present. This enables streaming to persist over network hitches.

When it's time to render a frame, the opponent's input has most likely arrived via the network. If so, the frame can then be rendered freely.

Players are truly synchronised as their games only proceed once all their inputs have been recognised. This is the main advantage of lockstep.

### Locking

If all players’ inputs aren’t received after D ticks, the game halts until those inputs arrive.

This also happens with video streaming. If the video data buffer is exhausted, the application has no choice but to pause and wait.

This causes stuttering when network conditions are unstable, which is a common scenario in online play. This is a crippling disadvantage of lockstep.

### Input delay

Recall that we run the game at a delay of D ticks.

D may be pre-defined or dynamically adapted to latency. A dynamic delay can help reduce unnecessary input lag in fast network conditions or add input lag to reduce freezing in bad network conditions. However, changes in the delay will require players to recalibrate muscle memory, which is inconvenient.

In this project, we pre-define D in the settings menu.

In any case, input delay makes gameplay feel unresponsive and is therefore a large downside of lockstep.

### Determinism

We only send input in lockstep.

This means that inputs must be deterministic so that all players end up with the same game state. This can be difficult to achieve, especially due to floating point imprecisions.

This project is susceptible to issues surrounding non-determinism due to floating point calculations in its physics.


## Game loop

[GameController.cs](./Assets/Scripts/Gameplay/GameController.cs)

```C#
void GameLoop(ushort currentTick)
{ 
    ushort simulationTick = TickService.Subtract(currentTick, Settings.InputDelayTicks);

    SelfPlayer.SendUnackedInputs(untilTickExclusive: currentTick);

    if (!PeerPlayer.HasInput(simulationTick))
    {
        Clock.Instance.PauseIncrementing();
        return;
    }

    Clock.Instance.ResumeIncrementing();

    SelfPlayer.WriteInput(currentTick);

    SelfPlayer.Simulate(simulationTick);
    PeerPlayer.Simulate(simulationTick);

    SelfPlayer.DisposeInputs(untilTickExclusive: simulationTick);
    PeerPlayer.DisposeInputs(untilTickExclusive: simulationTick);

    Physics2D.Simulate(TickService.TimeBetweenTicksSec);
}
```

On a single node, "self player" refers to the actor that is being controlled, whereas "peer player" refers to the actor representing the remote peer. We will use this terminology moving forwards.

The game loop is called every tick. The player sends and writes inputs for the current tick whereas the input delay is applied to the simulation. The game is paused if the peer's input hasn't arrived for the simulation tick.

We dispose of inputs to ensure that our circular input buffers wrap around nicely.

Note that although we call `Simulate()` on the players, the simulation only actually runs on the last call to `Physics2D.Simulate()`.


## Players

We adopt a conventional object-oriented approach.

Each player is accessed through a manager inherited from [Player.cs](./Assets/Scripts/Gameplay/Player/Manager/Player.cs). This manager defers calls to multiple sub-managers. We'll describe some of the more interesting ones.

### Player movement manager

[MovementManager.cs](./Assets/Scripts/Gameplay/Player/MovementManager.cs)

```C#
public void Simulate(ushort tick)
{
    float deltaTime = TickService.TimeBetweenTicksSec;

    m_CandidatePosition = m_RB2D.position;

    GroundCheck();
    ProposeVelocity(tick, deltaTime);
    AdjustVelocityForObstructions();

    // Move along the final m_CandidateVelocity
    m_CandidatePosition += m_CandidateVelocity * deltaTime;

    if (m_CandidatePosition != m_RB2D.position)
    {
        m_RB2D.MovePosition(m_CandidatePosition);
    }

    SetFacingDirection();
}
```

Movement is kinematic.

When `Simulate()` is called on a player such as from the game loop, it's this function that's actually being called.

The `ProposeVelocity()` function reads from the player's corresponding input manager.

Note that `m_RB2D.MovePosition()` is only resolved when `Physics2D.Simulate()` is called, which is only done in the main game loop.

### Player input manager

Each player stores a circular input buffer. This buffer is indexed by tick and stores a bitarray of inputs that were active during that tick.

[InputBuffer.cs](./Assets/Scripts/Gameplay/Player/Input/InputBuffer.cs)

```C#
// m_InputHistory[tick] = input bitarray during that tick
ushort[] m_InputHistory = new ushort[TickService.MaxTick];
ushort m_StartInclusive;
ushort m_EndExclusive;

public bool HasInput(ushort tick)
{
    return TickService.IsAfterOrEqual(tick, StartInclusive) && TickService.IsBefore(tick, EndExclusive);
}

// Same as GetButton/GetKey from Unity's old Input system,
// except tick-based instead of frame-based
public bool GetInput(ushort tick, ushort inputMask)
{
    Assert.IsTrue(HasInput(tick));
    return (m_InputHistory[tick] & inputMask) != 0;
}
```

Moving onwards, the self player is responsible for sending its inputs.

We send all inputs that haven’t been acknowledged. This prevents infinite halting in the case of packet loss. Players are able to fully reconstruct each others’ input histories.

[SelfInputManager.cs](./Assets/Scripts/Gameplay/Player/Input/SelfInputManager.cs)

```C#
public void SendUnackedInputs(ushort untilTickExclusive)
{
    List<ushort> inputs = new List<ushort>();

    for (ushort t = m_NextTickToSend; TickService.IsBefore(t, untilTickExclusive); t = TickService.Add(t, 1))
    {
        inputs.Add(m_InputBuffer.GetRawInput(t));
    }

    if (inputs.Count == 0)
    {
        return;
    }

    SendInputs(startTick: m_NextTickToSend, inputs: inputs.ToArray());
}

void OnMessageReceived(object sender, DarkRift.Client.MessageReceivedEventArgs e)
{
    using (Message message = e.GetMessage() as Message)
    {
        if (message.Tag == Tags.InputAck)
        {
            HandleInputAckMsg(sender, e);
        }
    }
}

void HandleInputAckMsg(object sender, DarkRift.Client.MessageReceivedEventArgs e)
{
    using (Message message = e.GetMessage())
    {
        InputAckMsg msg = message.Deserialize<InputAckMsg>();

        if (TickService.IsAfter(msg.ReceivedUntilTickExclusive, m_NextTickToSend))
        {
            m_NextTickToSend = msg.ReceivedUntilTickExclusive;
        }
    }
}
```

Next, the peer player is responsible for receiving inputs via the network and sending acks.

[PeerInputManager.cs](./Assets/Scripts/Gameplay/Player/Input/PeerInputManager.cs)

```C#
void OnMessageReceived(object sender, MessageReceivedEventArgs e)
{
    using (Message message = e.GetMessage() as Message)
    {
        if (message.Tag == Tags.Input)
        {
            HandleInputMsg(sender, e);
        }
    }
}

void HandleInputMsg(object sender, MessageReceivedEventArgs e)
{
    using (Message message = e.GetMessage())
    {
        InputMsg msg = message.Deserialize<InputMsg>();

        if (TickService.IsAfter(msg.EndTickExclusive, m_InputBuffer.EndExclusive))
        {
            ushort tick = msg.StartTick;

            foreach (ushort input in msg.Inputs)
            {
                // Don't overwrite inputs
                if (!m_InputBuffer.HasInput(tick))
                {
                    m_InputBuffer.WriteInput(tick, input);
                }

                tick = TickService.Add(tick, 1);
            }

            m_InputBuffer.EndExclusive = msg.EndTickExclusive;
        }

        SendInputAck(msg.EndTickExclusive);
    }
}
```


## Pre-game setup

We provide the following configuration options before starting the game.

![Settings menu](./ReadmeAssets/settings.png)

In-practise, the player ID, peer address and peer port would be provided by a matchmaking server. The player name would be available from an authentication step, and the self port would be automatically chosen.

We implement peer-to-peer with symmetric nodes. Each player has a server `m_SelfServer` and a client `m_SelfClient`. The client connects to the peer's `m_SelfServer` and is used as a read-only channel. This is mirrored on the peer's side and the resulting connection to our `m_SelfServer` is stored as `m_PeerClient` and used as a write-only channel.

[ConnectionManager.cs](./Assets/Scripts/Gameplay/ConnectionManager.cs)

```C#
XmlUnityServer m_SelfServer;
UnityClient m_SelfClient; // Connection from self client (us: reader) to peer server (writer)
IClient m_PeerClient; // Connection from peer client (reader) to self server (us: writer)

public IEnumerator Setup()
{
    if (m_SetupComplete)
    {
        Debug.LogError("Setup called multiple times");
    }

    m_SetupComplete = false;

    // Setup self server
    yield return SetupServer(Settings.SelfPort);

    // Connect self client to peer server
    yield return ConnectClient(Settings.PeerAddress, Settings.PeerPort);

    // Wait until peer client is connected to self server
    yield return new WaitUntil(() => m_PeerClient != null && (m_PeerClient.ConnectionState == ConnectionState.Connected));

    Debug.Log("Setup complete");

    m_SetupComplete = true;
}
```

This scheme could be scaled to more than two players by setting up a server-client pair for each additional player.

An alternative approach could involve using a dedicated server for routing messages between peers. This would allow each peer to just have one client connected to the hub server. However, this would introduce server upkeep costs, which would undermine the purpose of peer-to-peer.

This project does not implement NAT busting.


## Time

Our clock is written as follows.

[Clock.cs](./Assets/Scripts/Gameplay/Clock.cs)

```C#
IEnumerator NextTick()
{
    while (true)
    {
        if (m_JustUnpaused)
        {
            m_JustUnpaused = false;
            CurrentTick = TickService.Add(m_PausedAtTick, 1);
        }

        // Keep this first so that the start tick is ran
        TickUpdated?.Invoke(CurrentTick);

        yield return new WaitForSecondsRealtime(TickService.TimeBetweenTicksSec);

        // We implement pausing this way instead of restarting the coroutine
        // in order to preserve the original cadence
        if (!Paused)
        {
            IncrementCurrentTick();
        }
    }
}
```

We define the service below for handling tick arithmetic.

Note that we store ticks as ushorts and use a tickrate of 60 ticks per second, which means that overflow occurs approximately every 18 minutes. We avoid this by wrapping around tick values using modular arithmetic.

[TickService.cs](./Assets/Scripts/Services/TickService.cs)

```C#
public const ushort Tickrate = 60;
public const float TimeBetweenTicksSec = 1f / Tickrate;
public const ushort StartTick = 0;
public const ushort MaxTick = 65530; // A bit less than the actual maximum of a ushort so that incrementing won't overflow

static readonly ushort LargeTickThreshold = (ushort)(MaxTick-SecondsToTicks(100));
static readonly ushort SmallTickThreshold = SecondsToTicks(100);

public static ushort Add(ushort tick, int x)
{
    return (ushort)MathExtensions.Mod((int)tick + x, MaxTick);
}

public static bool IsBefore(ushort tick1, ushort tick2)
{
    // If tick1 is large and tick2 is small, then assume tick2 has
    // wrapped around and hence tick2 > tick1 => true

    // Similarly, if tick1 is small and tick2 is large, then assume
    // tick1 has wrapped around so tick1 > tick2 => false

    return IsLarge(tick1) && IsSmall(tick2)
        ? true
        : IsSmall(tick1) && IsLarge(tick2)
            ? false
            : tick1 < tick2;
}
```


## Network technicalities

We use TCP for sending initial player metadata (usernames) and UDP for everything else.

We simulate latency and packet loss by wrapping DarkRift's `SendMessage()` function in the following code.

[ConnectionManager.cs](./Assets/Scripts/Gameplay/ConnectionManager.cs)

```C#
public void SendMessage(Func<Message> createMessage, SendMode sendMode)
{
    Assert.IsTrue(m_PeerClient != null);
    Assert.IsTrue(m_PeerClient.ConnectionState == ConnectionState.Connected);

    StartCoroutine(SendMessageUnderSimulatedConditions(createMessage, sendMode));
}

IEnumerator SendMessageUnderSimulatedConditions(Func<Message> createMessage, SendMode sendMode)
{
    // Artificial latency
    if (Settings.ArtificialLatencyMs > 0)
    {
        yield return new WaitForSecondsRealtime(Settings.ArtificialLatencyMs / 1000f);
    }

    // Artificial packet loss
    if (sendMode == SendMode.Unreliable && RandomService.ReturnTrueWithProbability(Settings.ArtificialPacketLossPc))
    {
        yield break;
    }

    using (Message msg = createMessage())
    {
        if (!m_PeerClient.SendMessage(msg, sendMode))
        {
            Debug.Log("Failed to send message");
        }
    }
}
```

Here's an example that demonstrates serialisation and deserialisation. DarkRift provides a nice interface for these concerns.

[InputMsg.cs](./Assets/Scripts/Messages/InputMsg.cs)

```C#
public class InputMsg : IDarkRiftSerializable
{
    public ushort StartTick { get; private set; }
    public ushort[] Inputs { get; private set; }

    public int NumTicks => Inputs.Length;
    public ushort EndTickExclusive => TickService.Add(StartTick, NumTicks);

    public InputMsg() {}

    public InputMsg(ushort startTick, ushort[] inputs)
    {
        Assert.IsTrue(inputs.Length > 0);
        StartTick = startTick;
        Inputs = inputs;
    }

    public static Message CreateMessage(ushort startTick, ushort[] inputs)
    {
        return Message.Create(
            Tags.Input,
            new InputMsg(startTick, inputs)
        );
    }

    public void Deserialize(DeserializeEvent e)
    {
        StartTick = e.Reader.ReadUInt16();
        Inputs = e.Reader.ReadUInt16s();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(StartTick);
        e.Writer.Write(Inputs);
    }
}
```

As shown above, we also define a custom `CreateMessage()` function in each message class to streamline messaging.

Here's an example of how we send the above message using UDP.

[SelfInputManager.cs](./Assets/Scripts/Gameplay/Player/Input/SelfInputManager.cs)

```C#
void SendInputs(ushort startTick, ushort[] inputs)
{
    ConnectionManager.Instance.SendMessage(() => InputMsg.CreateMessage(startTick, inputs), SendMode.Unreliable);
}
```
