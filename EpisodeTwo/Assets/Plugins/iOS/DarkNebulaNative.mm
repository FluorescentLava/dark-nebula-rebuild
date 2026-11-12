#import <UIKit/UIKit.h>
#import <CoreHaptics/CoreHaptics.h>
#import <dispatch/dispatch.h>
#include <stdint.h>

static CHHapticEngine *hapticEngine;
static NSMutableArray *hapticPlayers;

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

static bool DarkNebulaStartPattern(CHHapticEngine *engine, CHHapticPattern *pattern)
{
    NSError *error = nil;
    id<CHHapticPatternPlayer> player = [engine createPlayerWithPattern:pattern error:&error];
    if (player == nil) return false;
    // The collection keeps the active player alive with and without ARC.
    if (hapticPlayers == nil) hapticPlayers = [[NSMutableArray alloc] init];
    [hapticPlayers removeAllObjects];
    [hapticPlayers addObject:player];
    return [player startAtTime:CHHapticTimeImmediate error:&error];
}

// Reuse the engine, but give each numeric tick/award its own finite 15 ms
// event. This continues for any counting duration without a repeating player.
static bool DarkNebulaTimedImpact(float intensity, float sharpness)
{
    CHHapticEngine *engine = DarkNebulaHapticEngine();
    if (engine == nil) return false;
    NSError *error = nil;
    CHHapticEventParameter *strength = [[CHHapticEventParameter alloc]
        initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intensity];
    CHHapticEventParameter *texture = [[CHHapticEventParameter alloc]
        initWithParameterID:CHHapticEventParameterIDHapticSharpness value:sharpness];
    CHHapticEvent *event = [[CHHapticEvent alloc]
        initWithEventType:CHHapticEventTypeHapticContinuous
        parameters:@[strength, texture] relativeTime:0 duration:0.015];
    CHHapticPattern *pattern = [[CHHapticPattern alloc]
        initWithEvents:@[event] parameters:@[] error:&error];
    if (pattern == nil) return false;
    return DarkNebulaStartPattern(engine, pattern);
}

extern "C" int DarkNebulaPlayHapticPattern(const int64_t *timing, const int32_t *strength,
                                          int count, float intensityScale)
{
    if (timing == nullptr || strength == nullptr || count <= 0) return 0;
    NSMutableArray<CHHapticEvent *> *events = [NSMutableArray array];
    double time = 0;
    float peakIntensity = 0;
    for (int i = 0; i < count; ++i)
    {
        double duration = timing[i] / 1000.0;
        float intensity = strength[i] / 255.0f * intensityScale;
        if (duration > 0 && intensity > 0)
        {
            peakIntensity = MAX(peakIntensity, intensity);
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
        if (pattern != nil && DarkNebulaStartPattern(engine, pattern)) return 1;
    }
    // Older hardware gets a scaled impact instead of the long system vibration.
    UIImpactFeedbackStyle style = peakIntensity < 0.4f ? UIImpactFeedbackStyleLight :
        peakIntensity < 0.8f ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy;
    static UIImpactFeedbackGenerator *feedback[3];
    if (feedback[style] == nil)
        feedback[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
    [feedback[style] impactOccurredWithIntensity:peakIntensity];
    [feedback[style] prepare];
    return 1;
}

extern "C" void DarkNebulaCancelHaptics()
{
    NSError *error = nil;
    for (id<CHHapticPatternPlayer> player in hapticPlayers)
        [player stopAtTime:CHHapticTimeImmediate error:&error];
    [hapticPlayers removeAllObjects];
}

extern "C" void DarkNebulaScoreTick()
{
    if (DarkNebulaTimedImpact(0.75f, 0.5f)) return;
    static UIImpactFeedbackGenerator *feedback;
    if (feedback == nil)
        feedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
    [feedback impactOccurredWithIntensity:1.0];
    [feedback prepare];
}

extern "C" void DarkNebulaStarAward(float intensity)
{
    if (DarkNebulaTimedImpact(intensity, 1.0f)) return;
    static UIImpactFeedbackGenerator *feedback;
    if (feedback == nil)
        feedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
    [feedback impactOccurredWithIntensity:intensity];
    [feedback prepare];
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
