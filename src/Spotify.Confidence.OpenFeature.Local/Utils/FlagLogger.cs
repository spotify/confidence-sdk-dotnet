
using System.Reflection;
using global::Confidence.Flags.Resolver.V1;
using global::Confidence.Flags.Resolver.V1.Events;
using Google.Protobuf.WellKnownTypes;
using RustGuest;
using Spotify.Confidence.OpenFeature.Local.Models;
using AppliedFlag = global::Confidence.Flags.Resolver.V1.Events.FlagAssigned.Types.AppliedFlag;
using AssignmentInfo = global::Confidence.Flags.Resolver.V1.Events.FlagAssigned.Types.AssignmentInfo;
using DefaultAssignment = global::Confidence.Flags.Resolver.V1.Events.FlagAssigned.Types.DefaultAssignment;
using DefaultAssignmentReason = global::Confidence.Flags.Resolver.V1.Events.FlagAssigned.Types.DefaultAssignment.Types.DefaultAssignmentReason;
using Sdk = global::Confidence.Flags.Resolver.V1.Sdk;
using SdkId = global::Confidence.Flags.Resolver.V1.SdkId;

namespace Spotify.Confidence.OpenFeature.Local.Utils;

public static class FlagLogger
{
    public static FlagAssigned CreateFlagAssigned(
        string resolveId, 
        IList<FlagToApply> flagsToApply, 
        AccountClient accountClient)
    {
        var clientInfo = new ClientInfo
        {
            Client = accountClient.Client.Name,
            ClientCredential = accountClient.ClientCredential.Name,
            Sdk = new Sdk {
                Id = SdkId.DotnetConfidence,
                Version = GetVersion()
            }
        };

        var builder = new FlagAssigned
        {
            ResolveId = resolveId,
            ClientInfo = clientInfo
        };

        foreach (var flag in flagsToApply)
        {
            var assignedFlag = flag.AssignedFlags;
            var assignedBuilder = new AppliedFlag
            {
                AssignmentId = assignedFlag.AssignmentId,
                Flag = assignedFlag.Flag,
                ApplyTime = new Timestamp
                {
                    Seconds = flag.SkewAdjustedAppliedTime.Seconds,
                    Nanos = flag.SkewAdjustedAppliedTime.Nanos
                },
                TargetingKey = assignedFlag.TargetingKey,
                TargetingKeySelector = assignedFlag.TargetingKeySelector,
                Rule = assignedFlag.Rule
            };

            // Add fallthrough assignments
            assignedBuilder.FallthroughAssignments.AddRange(assignedFlag.FallthroughAssignments);

            if (!string.IsNullOrEmpty(assignedFlag.Variant))
            {
                assignedBuilder.AssignmentInfo = new AssignmentInfo
                {
                    Segment = assignedFlag.Segment,
                    Variant = assignedFlag.Variant
                };
            }
            else
            {
                assignedBuilder.DefaultAssignment = new DefaultAssignment
                {
                    Reason = ResolveToAssignmentReason(assignedFlag.Reason)
                };
            }

            builder.Flags.Add(assignedBuilder);
        }

        return builder;
    }

    private static DefaultAssignmentReason ResolveToAssignmentReason(ResolveReason reason)
    {
        return reason switch
        {
            ResolveReason.NoSegmentMatch => DefaultAssignmentReason.NoSegmentMatch,
            ResolveReason.FlagArchived => DefaultAssignmentReason.FlagArchived,
            _ => DefaultAssignmentReason.Unspecified
        };
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "unknown";
    }
}