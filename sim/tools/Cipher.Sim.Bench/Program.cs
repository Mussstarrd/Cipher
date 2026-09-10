using System;
using System.Diagnostics;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

// Cipher swarm benchmark.
//   --smoke : tiny run for CI (exercises the pipeline, no timing claims)
//   default : 1k / 5k agent runs, reports ms per simulation tick.
// Numbers on shared CI runners are noisy — treat local/dedicated hardware
// as the source of truth for the perf gate (docs/05 §2).

bool smoke = Array.Exists(args, a => a == "--smoke");

if (smoke)
{
    RunScenario(agents: 200, steps: 60, label: "smoke");
    Console.WriteLine("SMOKE OK");
    return 0;
}

RunScenario(agents: 1_000, steps: 300, label: "1k");
RunScenario(agents: 5_000, steps: 300, label: "5k");
return 0;

static void RunScenario(int agents, int steps, string label)
{
    const float dt = 1f / 30f;

    // 64x64 arena with a serpentine to make pathing non-trivial.
    var map = new GridMap(64, 64);
    for (int y = 0; y < 50; y++) map.SetBlocked(20, y, true);
    for (int y = 14; y < 64; y++) map.SetBlocked(40, y, true);

    var field = new FlowField(map);
    var fieldTimer = Stopwatch.StartNew();
    field.Compute(60, 32);
    fieldTimer.Stop();

    var world = new AgentWorld(map, field, new SimConfig(), initialCapacity: agents);
    for (int i = 0; i < agents; i++)
        world.Spawn(new Vec2(1f + (i % 16) * 0.3f, 1f + (i / 16) * 0.15f), health: 10f);

    // Warmup (JIT), then measure.
    for (int s = 0; s < 10; s++) world.Step(dt);

    var timer = Stopwatch.StartNew();
    for (int s = 0; s < steps; s++)
    {
        world.Step(dt);
        if (s % 30 == 0)
            world.ApplyRadialDamage(new Vec2(30f, 30f), radius: 4f, damage: 2f);
    }
    timer.Stop();

    double msPerStep = timer.Elapsed.TotalMilliseconds / steps;
    Console.WriteLine(
        $"[{label}] agents={agents} steps={steps} " +
        $"flowfield={fieldTimer.Elapsed.TotalMilliseconds:F2}ms " +
        $"sim={msPerStep:F3}ms/step " +
        $"alive={world.AliveCount} reached={world.ReachedCount} kills={world.TotalKills} " +
        $"hash={world.StateHash():X16}");
}
