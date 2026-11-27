using System;
using System.Reflection;
using System.Globalization;

public static class FlightTrackerWrapper
{
    private const string AssemblyName = "FlightTracker";
    private const string ApiTypeName = "FlightTracker.FlightTrackerApi";

    private static bool assemblySearched;

    private static Type apiType;
    private static object apiInstance;

    private static FieldInfo instanceField;
    private static FieldInfo updateEventField;

    private static Func<double, string> convertUtToString;
    private static Func<string, int> getNumberOfFlights;
    private static Func<string, double> getRecordedMissionTimeSeconds;
    private static Func<string, double> getRecordedMissionTimeHours;
    private static Func<string, double> getLaunchTime;
    private static Func<string, int> getNumberOfWorldFirsts;
    private static Func<Vessel, double> vesselRealMet;
    private static Func<Guid, double> vesselRealMetGuid;

    private static bool available;

    /// <summary>
    /// Returns true if the FlightTracker API assembly is installed.
    /// Does not require the API instance to be initialized.
    /// </summary>
    public static bool IsInstalled
    {
        get
        {
            FindAssembly();
            return apiType != null;
        }
    }

    /// <summary>
    /// Returns true if FlightTracker is installed and its API
    /// instance and methods are available.
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
            return Initialize();
        }
    }

    /// <summary>
    /// Locate the FlightTracker assembly and API class.
    /// </summary>
    private static void FindAssembly()
    {
        if (assemblySearched)
            return;

        assemblySearched = true;

        try
        {
            foreach (var loadedAssembly in
                AssemblyLoader.loadedAssemblies)
            {
                Assembly assembly = loadedAssembly.assembly;

                if (!string.Equals(
                    assembly.GetName().Name,
                    AssemblyName,
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                apiType = assembly.GetType(ApiTypeName);

                if (apiType == null)
                    continue;

                instanceField = apiType.GetField(
                    "Instance",
                    BindingFlags.Public | BindingFlags.Static);

                updateEventField = apiType.GetField(
                    "OnFlightTrackerUpdated",
                    BindingFlags.Public | BindingFlags.Static);

                return;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning(
                "[FlightTrackerWrapper] Assembly search failed: " + ex);
        }
    }

    /// <summary>
    /// Initialize the wrapper. May be called repeatedly.
    /// If FlightTracker has not yet created its instance,
    /// initialization will be retried on the next call.
    /// </summary>
    public static bool Initialize()
    {
        if (available)
        {
            UnityEngine.Object unityObject =
                apiInstance as UnityEngine.Object;

            if (unityObject != null)
                return true;

            available = false;
            apiInstance = null;
        }

        FindAssembly();

        if (apiType == null || instanceField == null)
            return false;

        try
        {
            object instance = instanceField.GetValue(null);

            if (instance == null)
                return false;

            UnityEngine.Object unityObject =
                instance as UnityEngine.Object;

            if (unityObject == null)
                return false;

            apiInstance = instance;

            convertUtToString =
                Bind<Func<double, string>>(
                    "ConvertUtToString");

            getNumberOfFlights =
                Bind<Func<string, int>>(
                    "GetNumberOfFlights");

            getRecordedMissionTimeSeconds =
                Bind<Func<string, double>>(
                    "GetRecordedMissionTimeSeconds");

            getRecordedMissionTimeHours =
                Bind<Func<string, double>>(
                    "GetRecordedMissionTimeHours");

            getLaunchTime =
                Bind<Func<string, double>>(
                    "GetLaunchTime");

            getNumberOfWorldFirsts =
                Bind<Func<string, int>>(
                    "GetNumberOfWorldFirsts");

            vesselRealMet =
                Bind<Func<Vessel, double>>(
                    "VesselRealMet");

            vesselRealMetGuid =
                Bind<Func<Guid, double>>(
                    "VesselRealMet");

            available =
                convertUtToString != null &&
                getNumberOfFlights != null &&
                getRecordedMissionTimeSeconds != null &&
                getRecordedMissionTimeHours != null &&
                getLaunchTime != null &&
                getNumberOfWorldFirsts != null &&
                vesselRealMet != null &&
                vesselRealMetGuid != null;

            if (available)
            {
                UnityEngine.Debug.Log(
                    "[FlightTrackerWrapper] API initialized");
            }

            return available;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning(
                "[FlightTrackerWrapper] Initialization failed: " + ex);

            return false;
        }
    }

    /// <summary>
    /// Bind a public instance method to the FlightTracker
    /// singleton using the delegate's parameter types.
    /// </summary>
    private static T Bind<T>(string methodName)
        where T : class
    {
        MethodInfo invoke = typeof(T).GetMethod("Invoke");

        ParameterInfo[] parameters = invoke.GetParameters();

        Type[] parameterTypes = new Type[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            parameterTypes[i] = parameters[i].ParameterType;

        MethodInfo method = apiType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance,
            null,
            parameterTypes,
            null);

        if (method == null)
        {
            UnityEngine.Debug.LogWarning(
                "[FlightTrackerWrapper] Method not found: " +
                methodName);

            return null;
        }

        if (method.ReturnType != invoke.ReturnType)
        {
            UnityEngine.Debug.LogWarning(
                "[FlightTrackerWrapper] Incorrect return type: " +
                methodName);

            return null;
        }

        return Delegate.CreateDelegate(
            typeof(T),
            apiInstance,
            method,
            false) as T;
    }

    /// <summary>
    /// Safely execute an API call.
    /// </summary>
    private static TResult SafeCall<TResult>(
        Func<TResult> function,
        TResult fallback)
    {
        if (!IsAvailable)
            return fallback;

        try
        {
            return function();
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    #region FlightTracker API

    /// <summary>
    /// Convert a UT duration into a formatted hours string.
    /// </summary>
    public static string ConvertUtToString(double time)
    {
        return SafeCall(
            () => convertUtToString(time),
            FormatHours(time));
    }

    /// <summary>
    /// Alias matching the original documented method name.
    /// </summary>
    public static string ConvertUTToString(double time)
    {
        return ConvertUtToString(time);
    }

    /// <summary>
    /// Returns the number of successfully completed flights.
    /// </summary>
    public static int GetNumberOfFlights(string kerbalName)
    {
        return SafeCall(
            () => getNumberOfFlights(kerbalName),
            0);
    }

    /// <summary>
    /// Returns recorded mission time in seconds.
    /// Updated upon vessel recovery.
    /// </summary>
    public static double GetRecordedMissionTimeSeconds(
        string kerbalName)
    {
        return SafeCall(
            () => getRecordedMissionTimeSeconds(kerbalName),
            0.0);
    }

    /// <summary>
    /// Returns recorded mission time in hours.
    /// </summary>
    public static double GetRecordedMissionTimeHours(
        string kerbalName)
    {
        return SafeCall(
            () => getRecordedMissionTimeHours(kerbalName),
            0.0);
    }

    /// <summary>
    /// Returns the UT of the Kerbal's most recent launch.
    /// </summary>
    public static double GetLaunchTime(string kerbalName)
    {
        return SafeCall(
            () => getLaunchTime(kerbalName),
            0.0);
    }

    /// <summary>
    /// Returns the number of World Firsts accomplished
    /// by the specified Kerbal.
    /// </summary>
    public static int GetNumberOfWorldFirsts(string kerbalName)
    {
        return SafeCall(
            () => getNumberOfWorldFirsts(kerbalName),
            0);
    }

    /// <summary>
    /// Returns the vessel's FlightTracker mission time.
    /// Falls back to stock mission time.
    /// </summary>
    public static double VesselRealMet(Vessel vessel)
    {
        if (vessel == null)
            return 0.0;

        return SafeCall(
            () => vesselRealMet(vessel),
            vessel.missionTime);
    }

    /// <summary>
    /// Returns the vessel's FlightTracker mission time
    /// using its GUID.
    /// </summary>
    public static double VesselRealMet(Guid vesselId)
    {
        if (IsAvailable)
        {
            try
            {
                return vesselRealMetGuid(vesselId);
            }
            catch (Exception)
            {
                // Fall through to stock mission time.
            }
        }

        return GetStockMissionTime(vesselId);
    }

    #endregion

    #region FlightTracker Events

    /// <summary>
    /// Returns the FlightTracker update event, or null
    /// if FlightTracker is unavailable.
    /// </summary>
    public static EventData<ProtoCrewMember>
        OnFlightTrackerUpdated
    {
        get
        {
            if (!IsAvailable || updateEventField == null)
                return null;

            try
            {
                return updateEventField.GetValue(null)
                    as EventData<ProtoCrewMember>;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Register a callback for FlightTracker updates.
    /// Returns true if the callback was registered.
    /// </summary>
    public static bool AddUpdateCallback(
        EventData<ProtoCrewMember>.OnEvent callback)
    {
        if (callback == null)
            return false;

        EventData<ProtoCrewMember> updateEvent =
            OnFlightTrackerUpdated;

        if (updateEvent == null)
            return false;

        updateEvent.Add(callback);

        return true;
    }

    /// <summary>
    /// Unregister a previously registered callback.
    /// </summary>
    public static void RemoveUpdateCallback(
        EventData<ProtoCrewMember>.OnEvent callback)
    {
        if (callback == null)
            return;

        EventData<ProtoCrewMember> updateEvent =
            OnFlightTrackerUpdated;

        if (updateEvent != null)
            updateEvent.Remove(callback);
    }

    #endregion

    #region Fallback Methods

    /// <summary>
    /// Find a vessel by GUID and return its stock mission time.
    /// </summary>
    private static double GetStockMissionTime(Guid vesselId)
    {
        if (FlightGlobals.Vessels == null)
            return 0.0;

        foreach (Vessel vessel in FlightGlobals.Vessels)
        {
            if (vessel != null && vessel.id == vesselId)
                return vessel.missionTime;
        }

        return 0.0;
    }

    /// <summary>
    /// Replicates FlightTracker's actual time formatting
    /// implementation.
    /// </summary>
    private static string FormatHours(double time)
    {
        time = time / 60 / 60;
        time = (int)Math.Floor(time);

        string timeString =
            time.ToString(CultureInfo.CurrentCulture);

        if (timeString.Length > 4)
        {
            timeString = timeString.Substring(
                0, timeString.Length - 3) + "k";
        }

        return timeString;
    }

    #endregion
}