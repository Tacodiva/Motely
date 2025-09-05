using System.Diagnostics;

namespace Motely;

public struct MotelySingleBossStream(MotelySinglePrngStream prngStream)
{
    public MotelySinglePrngStream PrngStream = prngStream;
}

unsafe ref partial struct MotelySingleSearchContext
{
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleBossStream CreateBossStream()
    {
        return new(CreatePrngStream(MotelyPrngKeys.Boss));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyBossBlind GetBossForAnte(ref MotelySingleBossStream stream, int ante, ref MotelyRunState state)
    {
        // 23 is the maximum number of boss blinds in the pool, because there is 23 non-finisher boss blinds
        const int maxPoolLength = 23; 

        MotelyBossBlind* pool = stackalloc MotelyBossBlind[maxPoolLength];
        int poolLength = 0;

        if (ante % 8 == 0)
        {
            // Finisher boss blind
            for (int i = 0; i < MotelyBossBlindExt.FinisherBossBlinds.Length; i++)
            {
                MotelyBossBlind boss = MotelyBossBlindExt.FinisherBossBlinds[i];

                if (state.HasSeenBoss(boss)) continue;

                Debug.Assert(poolLength < maxPoolLength);

                pool[poolLength++] = boss;
            }

            if (poolLength == 0)
            {
                state.ResetFinisherBosses();

                for (int i = 0; i < MotelyBossBlindExt.FinisherBossBlinds.Length; i++)
                {
                    MotelyBossBlind boss = MotelyBossBlindExt.FinisherBossBlinds[i];
                    // None of them should be seen because we reset the state
                    Debug.Assert(!state.HasSeenBoss(boss));
                    Debug.Assert(poolLength < maxPoolLength);

                    pool[poolLength++] = boss;
                }
            }
        }
        else
        {
            // Normal boss blind
            for (int i = 0; i < MotelyBossBlindExt.NormalBossBlinds.Length; i++)
            {
                MotelyBossBlind boss = MotelyBossBlindExt.NormalBossBlinds[i];

                if (state.HasSeenBoss(boss)) continue;
                if (ante < boss.GetBossMinAnte()) continue;

                Debug.Assert(poolLength < maxPoolLength);

                pool[poolLength++] = boss;
            }

            if (poolLength == 0)
            {
                state.ResetNormalBosses();

                for (int i = 0; i < MotelyBossBlindExt.NormalBossBlinds.Length; i++)
                {
                    MotelyBossBlind boss = MotelyBossBlindExt.NormalBossBlinds[i];

                    if (ante < boss.GetBossMinAnte()) continue;

                    Debug.Assert(!state.HasSeenBoss(boss));
                    Debug.Assert(poolLength < maxPoolLength);

                    pool[poolLength++] = boss;
                }
            }
        }

        MotelyBossBlind selectedBoss = pool[GetNextRandomInt(ref stream.PrngStream, 0, poolLength)];

        state.SeeBoss(selectedBoss);

        return selectedBoss;
    }
}