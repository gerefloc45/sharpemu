// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

namespace SharpEmu.HLE;

public readonly record struct GuestThreadStartRequest(
    ulong ThreadHandle,
    ulong EntryPoint,
    ulong Argument,
    ulong AttributeAddress,
    string Name);

public interface IGuestThreadScheduler
{
    bool TryStartThread(CpuContext creatorContext, GuestThreadStartRequest request, out string? error);

    void Pump(CpuContext callerContext, string reason);
}

public static class GuestThreadExecution
{
    [ThreadStatic]
    private static ulong _currentGuestThreadHandle;

    [ThreadStatic]
    private static string? _pendingBlockReason;

    [ThreadStatic]
    private static bool _pendingEntryExit;

    [ThreadStatic]
    private static int _pendingEntryExitStatus;

    [ThreadStatic]
    private static string? _pendingEntryExitReason;

    public static IGuestThreadScheduler? Scheduler { get; set; }

    public static bool IsGuestThread => _currentGuestThreadHandle != 0;

    public static ulong CurrentGuestThreadHandle => _currentGuestThreadHandle;

    public static ulong EnterGuestThread(ulong threadHandle)
    {
        var previous = _currentGuestThreadHandle;
        _currentGuestThreadHandle = threadHandle;
        _pendingBlockReason = null;
        _pendingEntryExit = false;
        _pendingEntryExitStatus = 0;
        _pendingEntryExitReason = null;
        return previous;
    }

    public static void RestoreGuestThread(ulong previousThreadHandle)
    {
        _currentGuestThreadHandle = previousThreadHandle;
        _pendingBlockReason = null;
        _pendingEntryExit = false;
        _pendingEntryExitStatus = 0;
        _pendingEntryExitReason = null;
    }

    public static bool RequestCurrentThreadBlock(string reason)
    {
        if (!IsGuestThread)
        {
            return false;
        }

        _pendingBlockReason = string.IsNullOrWhiteSpace(reason) ? "guest_thread_blocked" : reason;
        return true;
    }

    public static bool TryConsumeCurrentThreadBlock(out string reason)
    {
        reason = _pendingBlockReason ?? string.Empty;
        if (string.IsNullOrEmpty(reason))
        {
            return false;
        }

        _pendingBlockReason = null;
        return true;
    }

    public static void RequestCurrentEntryExit(string reason, int status)
    {
        _pendingEntryExit = true;
        _pendingEntryExitStatus = status;
        _pendingEntryExitReason = string.IsNullOrWhiteSpace(reason) ? "guest_entry_exit" : reason;
    }

    public static bool TryConsumeCurrentEntryExit(out int status, out string reason)
    {
        status = _pendingEntryExitStatus;
        reason = _pendingEntryExitReason ?? string.Empty;
        if (!_pendingEntryExit)
        {
            return false;
        }

        _pendingEntryExit = false;
        _pendingEntryExitStatus = 0;
        _pendingEntryExitReason = null;
        return true;
    }
}
