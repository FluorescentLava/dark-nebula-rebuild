#import <UIKit/UIKit.h>
#import <CoreHaptics/CoreHaptics.h>
#import <dispatch/dispatch.h>
#include <stdint.h>

static CHHapticEngine *hapticEngine;
static NSMutableArray *hapticPlayers;
static NSUInteger fallbackGeneration;
static dispatch_source_t scoreFallbackTimer;

static CHHapticEngine *DarkNebulaHapticEngine()
{
    if (![CHHapticEngine capabilitiesForHardware].supportsHaptics) return nil;
    NSError *error = nil;
    if (hapticEngine == nil)
    {
        hapticEngine = [[CHHapticEngine alloc] initAndReturnError:&error];
        if (hapticEngine == nil) return nil;
        hapticEngine.playsHapticsOnly = YES;
        hapticEngine.autoShutdownEnabled = YES;
    }
    if (![hapticEngine startAndReturnError:&error])
    {
        hapticEngine = nil;
        return nil;
    }
    return hapticEngine;
}

static bool DarkNebulaStartPattern(CHHapticEngine *engine, CHHapticPattern *pattern, bool repeating, double duration)
{
    NSError *error = nil;
    id<CHHapticPatternPlayer> player;
    if (repeating)
    {
        id<CHHapticAdvancedPatternPlayer> loop = [engine createAdvancedPlayerWithPattern:pattern error:&error];
        if (loop == nil) return false;
        loop.loopEnabled = YES;
        loop.loopEnd = duration;
        player = loop;
    }
    else player = [engine createPlayerWithPattern:pattern error:&error];
    if (player == nil) return false;
    // The collection keeps the active player alive with and without ARC.
    if (hapticPlayers == nil) hapticPlayers = [[NSMutableArray alloc] init];
    [hapticPlayers removeAllObjects];
    [hapticPlayers addObject:player];
    return [player startAtTime:CHHapticTimeImmediate error:&error];
}

static void DarkNebulaImpact(float intensity)
{
    UIImpactFeedbackStyle style = intensity < 0.4f ? UIImpactFeedbackStyleLight :
        intensity < 0.8f ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy;
    static UIImpactFeedbackGenerator *feedback[3];
    if (feedback[style] == nil)
        feedback[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
    [feedback[style] impactOccurredWithIntensity:intensity];
    [feedback[style] prepare];
}

extern "C" int DarkNebulaPlayHapticPattern(const int64_t *timing, const int32_t *strength,
                                          int count, float intensityScale, int repeating)
{
    if (timing == nullptr || strength == nullptr || count <= 0) return 0;
    NSUInteger generation = ++fallbackGeneration;
    NSMutableArray<CHHapticEvent *> *events = [NSMutableArray array];
    double time = 0;
    float peakIntensity = 0;
    for (int i = 0; i < count; ++i)
    {
        double duration = timing[i] / 1000.0;
        float intensity = strength[i] / 255.0f * intensityScale;
        peakIntensity = MAX(peakIntensity, intensity);
        // Include the silent segment in looping patterns so loopEnd covers
        // the 15 ms rest as well as the 15 ms counting pulse.
        if (duration > 0 && (intensity > 0 || repeating))
        {
            CHHapticEventParameter *level = [[CHHapticEventParameter alloc]
                initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intensity];
            CHHapticEventParameter *texture = [[CHHapticEventParameter alloc]
                initWithParameterID:CHHapticEventParameterIDHapticSharpness value:0.5f];
            [events addObject:[[CHHapticEvent alloc]
                initWithEventType:CHHapticEventTypeHapticContinuous
                parameters:@[level, texture] relativeTime:time duration:duration]];
        }
        time += duration;
    }
    if (events.count == 0) return 0;
    CHHapticEngine *engine = DarkNebulaHapticEngine();
    if (engine != nil)
    {
        NSError *error = nil;
        CHHapticPattern *pattern = [[CHHapticPattern alloc]
            initWithEvents:events parameters:@[] error:&error];
        if (pattern != nil && DarkNebulaStartPattern(engine, pattern, repeating != 0, time)) return 1;
    }
    if (repeating)
    {
        scoreFallbackTimer = dispatch_source_create(DISPATCH_SOURCE_TYPE_TIMER, 0, 0, dispatch_get_main_queue());
        dispatch_source_set_timer(scoreFallbackTimer, DISPATCH_TIME_NOW, (uint64_t)(time * NSEC_PER_SEC), NSEC_PER_MSEC);
        dispatch_source_set_event_handler(scoreFallbackTimer, ^{
            if (generation == fallbackGeneration) DarkNebulaImpact(peakIntensity);
        });
        dispatch_resume(scoreFallbackTimer);
        return 1;
    }
    // Preserve pulse timing on older hardware too. Each segment uses its
    // light/medium/heavy impact, and cancellation invalidates pending blocks.
    double delay = 0;
    for (int i = 0; i < count; ++i)
    {
        double duration = timing[i] / 1000.0;
        float intensity = MIN(1.0f, MAX(0.0f, strength[i] / 255.0f * intensityScale));
        if (duration > 0 && intensity > 0)
        {
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(delay * NSEC_PER_SEC)),
                           dispatch_get_main_queue(), ^{
                if (generation == fallbackGeneration) DarkNebulaImpact(intensity);
            });
        }
        delay += duration;
    }
    return 1;
}

extern "C" void DarkNebulaCancelHaptics()
{
    ++fallbackGeneration;
    if (scoreFallbackTimer != nil)
    {
        dispatch_source_cancel(scoreFallbackTimer);
        scoreFallbackTimer = nil;
    }
    NSError *error = nil;
    for (id<CHHapticPatternPlayer> player in hapticPlayers)
        [player stopAtTime:CHHapticTimeImmediate error:&error];
    [hapticPlayers removeAllObjects];
}

extern "C" void DarkNebulaOpenEpisodeTwo()
{
    UIApplication *application = [UIApplication sharedApplication];
    [application openURL:[NSURL URLWithString:@"darknebula-episode2://open"]
                 options:@{}
       completionHandler:^(BOOL opened) {
        if (!opened)
            dispatch_async(dispatch_get_main_queue(), ^{
                [application openURL:[NSURL URLWithString:@"https://github.com/FluorescentLava/dark-nebula-rebuild/releases"]
                             options:@{} completionHandler:nil];
            });
    }];
}
