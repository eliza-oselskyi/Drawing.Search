using System;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Infrastructure;

public static class TestModeServiceLocator
{
    private static ITestModeService? _testModeService;

    public static void Initialize(ITestModeService testModeService)
    {
        _testModeService = testModeService;
    }

    public static ITestModeService Current =>
        _testModeService ?? throw new InvalidOperationException("Test mode service not initialized.");
}