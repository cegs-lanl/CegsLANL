namespace AeonHacs.Components;

public partial class CegsLANL : Cegs
{
    #region HacsComponent
    [HacsConnect]
    protected override void Connect()
    {
        base.Connect();

        TF = Find<Section>("TF");
        TF_IP1 = Find<Section>("TF_IP1");
        FTG_IP1 = Find<Section>("FTG_IP1");

        TF1 = Find<TcpTubeFurnace>("TF1");
        FTG_TFFlowManager = Find<FlowManager>("FTG_TFFlowManager");
        TFFlowManager = Find<FlowManager>("TFFlowManager");
    }
    #endregion HacsComponent

    #region System configuration
    #region HacsComponents
    /// <summary>
    /// Tube furnace section
    /// </summary>
    public ISection TF { get; set; }

    /// <summary>
    /// Tube furnace..Inlet Port 1 section
    /// </summary>
    public ISection TF_IP1 { get; set; }

    /// <summary>
    /// Flow-Through Gas..Tube Furnace..Inlet Port 1 section
    /// </summary>
    public ISection FTG_IP1 { get; set; }

    /// <summary>
    /// Tube furnace
    /// </summary>
    public TcpTubeFurnace TF1 { get; set; }

    /// <summary>
    /// Flow manager for gas (He or O2) into the tube furnace.
    /// </summary>        
    public FlowManager FTG_TFFlowManager { get; set; }

    /// <summary>
    /// Flow manager for gas out of the tube furnace.
    /// </summary>
    public FlowManager TFFlowManager { get; set; }

    #endregion HacsComponents
    #endregion System configuration

    #region Periodic system activities & maintenance
    #endregion Periodic system activities & maintenance

    #region Process Management

    protected override void BuildProcessDictionary()
    {
        Separators.Clear();

        // Running samples
        ProcessDictionary["Run samples"] = RunSamples;
        Separators.Add(ProcessDictionary.Count);

        // Preparation for running samples
        ProcessDictionary["Prepare GRs for new iron and desiccant"] = PrepareGRsForService;
        ProcessDictionary["Precondition GR iron"] = PreconditionGRs;
        ProcessDictionary["Replace iron in sulfur traps"] = ChangeSulfurFe;
        //ProcessDictionary["Service d13C ports"] = Service_d13CPorts;
        //ProcessDictionary["Load empty d13C ports"] = LoadEmpty_d13CPorts;
        //ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;
        Separators.Add(ProcessDictionary.Count);

        ProcessDictionary["Prepare carbonate sample for acid"] = PrepareCarbonateSample;
        ProcessDictionary["Load acidified carbonate sample"] = LoadCarbonateSample;
        Separators.Add(ProcessDictionary.Count);

        // Open line
        ProcessDictionary["Open and evacuate line"] = OpenLine;
        ProcessDictionary["Open and evacuate TF and VS1"] = OpenTF_VS1;
        Separators.Add(ProcessDictionary.Count);

        // Main process continuations
        ProcessDictionary["Collect, etc."] = CollectEtc;
        ProcessDictionary["Extract, etc."] = ExtractEtc;
        ProcessDictionary["Measure, etc."] = MeasureEtc;
        ProcessDictionary["Graphitize, etc."] = GraphitizeEtc;
        Separators.Add(ProcessDictionary.Count);

        // Top-level steps for main process sequence
        ProcessDictionary["Admit sealed CO2 to InletPort"] = AdmitSealedCO2IP;
        ProcessDictionary["Collect CO2 from InletPort"] = Collect;
        ProcessDictionary["Extract"] = Extract;
        ProcessDictionary["Measure"] = Measure;
        ProcessDictionary["Discard excess CO2 by splits"] = DiscardSplit;
        ProcessDictionary["Remove sulfur"] = RemoveSulfur;
        ProcessDictionary["Dilute small sample"] = Dilute;
        ProcessDictionary["Graphitize aliquots"] = GraphitizeAliquots;
        Separators.Add(ProcessDictionary.Count);

        // Secondary-level process sub-steps
        ProcessDictionary["Evacuate Inlet Port"] = EvacuateIP;
        ProcessDictionary["Flush Inlet Port"] = FlushIP;
        ProcessDictionary["Admit O2 into Inlet Port"] = AdmitIPO2;
        ProcessDictionary["Heat Quartz and Open Line"] = HeatQuartzOpenLine;
        ProcessDictionary["Turn off IP furnaces"] = TurnOffIPFurnaces;
        ProcessDictionary["Discard IP gases"] = DiscardIPGases;
        ProcessDictionary["Close IP"] = CloseIP;
        ProcessDictionary["Start collecting"] = StartCollecting;
        ProcessDictionary["Clear collection conditions"] = ClearCollectionConditions;
        ProcessDictionary["Collect until condition met"] = CollectUntilConditionMet;
        ProcessDictionary["Stop collecting"] = StopCollecting;
        ProcessDictionary["Stop collecting after bleed down"] = StopCollectingAfterBleedDown;
        ProcessDictionary["Evacuate and Freeze VTT"] = FreezeVtt;
        ProcessDictionary["Admit Dead CO2 into MC"] = AdmitDeadCO2;
        ProcessDictionary["Purify CO2 in MC"] = CleanupCO2InMC;
        ProcessDictionary["Discard MC gases"] = DiscardMCGases;
        ProcessDictionary["Divide sample into aliquots"] = DivideAliquots;
        Separators.Add(ProcessDictionary.Count);

        // Granular inlet port & sample process control
        ProcessDictionary["Turn on quartz furnace"] = TurnOnIpQuartzFurnace;
        ProcessDictionary["Turn off quartz furnace"] = TurnOffIpQuartzFurnace;
        ProcessDictionary["Disable sample setpoint ramping"] = DisableIpRamp;
        ProcessDictionary["Enable sample setpoint ramping"] = EnableIpRamp;
        ProcessDictionary["Turn on sample furnace"] = TurnOnIpSampleFurnace;
        ProcessDictionary["Adjust sample setpoint"] = AdjustIpSetpoint;
        ProcessDictionary["Adjust sample ramp rate"] = AdjustIpRampRate;
        ProcessDictionary["Wait for sample to rise to setpoint"] = WaitIpRiseToSetpoint;
        ProcessDictionary["Wait for sample to fall to setpoint"] = WaitIpFallToSetpoint;
        ProcessDictionary["Turn off sample furnace"] = TurnOffIpSampleFurnace;
        Separators.Add(ProcessDictionary.Count);

        // General-purpose process control actions
        ProcessDictionary["Wait for timer"] = WaitForTimer;
        ProcessDictionary["Wait for operator"] = WaitForOperator;
        Separators.Add(ProcessDictionary.Count);

        // Transferring CO2
        ProcessDictionary["Transfer CO2 from CT to VTT"] = TransferCO2FromCTToVTT;
        ProcessDictionary["Transfer CO2 from MC to VTT"] = TransferCO2FromMCToVTT;
        ProcessDictionary["Transfer CO2 from MC to GR"] = TransferCO2FromMCToGR;
        ProcessDictionary["Transfer CO2 from prior GR to MC"] = TransferCO2FromGRToMC;
        Separators.Add(ProcessDictionary.Count);

        // Flow control steps
        ProcessDictionary["No IP flow"] = NoIpFlow;
        ProcessDictionary["Use IP flow"] = UseIpFlow;
        ProcessDictionary["Backfill TF with He"] = BackfillTF1WithHe;
        ProcessDictionary["Notify to load TF"] = LoadTF;
        ProcessDictionary["Admit O2 to TF"] = AdmitO2toTF;
        ProcessDictionary["Open TF to IP1"] = OpenTF_IP1;            
        ProcessDictionary["Start collecting"] = StartCollecting;
        ProcessDictionary["Clear collection conditions"] = ClearCollectionConditions;
        ProcessDictionary["Collect until condition met"] = CollectUntilConditionMet;
        ProcessDictionary["Stop collecting"] = StopCollecting;
        ProcessDictionary["Stop collecting after bleed down"] = StopCollectingAfterBleedDown;
        Separators.Add(ProcessDictionary.Count);

        // Flow control sub-steps
        ProcessDictionary["Start flow through to trap"] = StartFlowThroughToTrap;
        ProcessDictionary["Start flow through to waste"] = StartFlowThroughToWaste;
        ProcessDictionary["Stop flow-through gas"] = StopFlowThroughGas;
        Separators.Add(ProcessDictionary.Count);

        // d13C port service routines
        //ProcessDictionary["Empty completed d13C ports"] = EmptyCompleted_d13CPorts;
        //ProcessDictionary["Thaw frozen d13C ports"] = ThawFrozen_d13CPorts;
        //ProcessDictionary["Load empty d13C ports"] = LoadEmpty_d13CPorts;
        //ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;
        //Separators.Add(ProcessDictionary.Count);

        // Utilities (generally not for sample processing)
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Exercise all Opened valves"] = ExerciseAllValves;
        ProcessDictionary["Close all Opened valves"] = CloseAllValves;
        ProcessDictionary["Exercise all LN Manifold valves"] = ExerciseLNValves;
        ProcessDictionary["Calibrate all multi-turn valves"] = CalibrateRS232Valves;
        ProcessDictionary["Measure MC volume (KV in MCP2)"] = MeasureVolumeMC;
        ProcessDictionary["Measure valve volumes (plug in MCP2)"] = MeasureValveVolumes;
        ProcessDictionary["Measure remaining chamber volumes"] = MeasureRemainingVolumes;
        ProcessDictionary["Check GR H2 density ratios"] = CalibrateGRH2;
        ProcessDictionary["Measure Extraction efficiency"] = MeasureExtractEfficiency;
        ProcessDictionary["Measure IP collection efficiency"] = MeasureIpCollectionEfficiency;

        // Test functions
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Test"] = Test;
        base.BuildProcessDictionary();
    }

    #region OpenLine


    /// <summary>
    /// Open and evacuate the chambers normally serviced by VacuumSystem1 including the TF
    /// </summary>
    protected virtual void OpenTF_VS1()
    {
        ProcessStep.Start("Open and evacuate TF and VS1");
        TF.Evacuate(IpEvacuationPressure);
        TF_IP1.Open();
        Find<InletPort>("IP1").Open();
        OpenLine(VacuumSystem1);
    }


    #endregion OpenLine


    #region Process Control Parameters

    /// <summary>
    /// The desired Tube Furnace  pressure, used for filling or flow management.
    /// </summary>
    public double TfPressureTarget => GetParameter("TfPressureTarget");

    #endregion Process Control Parameters


    #region Process Control Properties

    public virtual bool IpIsTubeFurnace => InletPort.SampleFurnace is TubeFurnace;

    /// <summary>
    /// Provide a flow of oxygen through the Inlet Port to combust the sample,
    /// instead of a fixed pressure.
    /// </summary>
    public virtual bool NeedIpFlow { get; set; } = false;

    #endregion Process Control Properties


    #region Process Steps


    /// <summary>
    /// Use a flow of oxygen through the Inlet Port to combust the sample.
    /// </summary>
    protected virtual void UseIpFlow() => NeedIpFlow = true;

    /// <summary>
    /// Provide a fixed amount (pressure) of oxygen into the Inlet Port
    /// to combust the sample.
    /// </summary>
    protected virtual void NoIpFlow() => NeedIpFlow = false;

    /// <summary>
    /// Fill the Tube Furnace chamber with helium in preparation
    /// for opening.
    /// </summary>
    protected virtual void BackfillTF1WithHe()
    {
        ProcessStep.Start($"Fill {InletPort.Name} to {Ambient.Pressure:0} Torr He");
        
        // Need to manage FTG gas source valve manually,
        // because we want the shutoff to be
        // downstream of the flow valve.
        var he = Find<IValve>("vHe_FTG");

        FTG_IP1.Isolate();
        var gs = InertGasSupply(TF);
        he.OpenWait();
        gs.FlowPressurize(Ambient.Pressure);
        gs.ShutOff();
        he.CloseWait();

        ProcessStep.End();
    }

    /// <summary>
    /// Notify the operator to load the tube furnace and
    /// wait for their 'Ok" to continue.
    /// </summary>
    protected virtual void LoadTF()
    {
        Pause("Ready for operator", "Load the Tube Furnace and seal it closed.");
    }

    /// <summary>
    /// Evacuate the Inlet Port to 'OkPressure'.
    /// </summary>
    protected override void EvacuateIP()
    {
        ProcessStep.Start($"Evacuate {InletPort.Name}");
        
        if (IpIsTubeFurnace)
            TF_IP1.Open();
        base.EvacuateIP(IpEvacuationPressure);

        ProcessStep.End();
    }

    /// <summary>
    /// Admit O2 into the tube furnace to reach a pressure of 
    /// TfPressureTarget. 
    /// </summary>
    protected virtual void AdmitO2toTF()
    {
        ProcessStep.Start($"Admit {TfPressureTarget:0} Torr O2 into {InletPort.Name}");

        var gs = GasSupply("O2", TF);
        // Need to manage FTG gas source valve manually,
        // because we want the shutoff (vFTG_TF) to be
        // downstream of the flow valve.
        var o2 = Find<IValve>("vO2_FTG");
        o2.OpenWait();
        gs.FlowPressurize(TfPressureTarget);        // normalization not possible
        o2.CloseWait();

        ProcessStep.End();
    }

    /// <summary>
    /// Start flowing O2 through the Inlet Port and the (warm) coil trap to vacuum.
    /// </summary>
    protected virtual void StartFlowThroughToWaste() => StartFlowThrough(false);

    /// <summary>
    /// Start flowing O2 through the Inlet Port and the frozen coil trap.
    /// </summary>
    protected virtual void StartFlowThroughToTrap() => StartFlowThrough(true);

    /// <summary>
    /// Start flowing O2 through the Inlet Port.
    /// TODO: waste through analyzer for RPO samples (not TF, though)
    /// </summary>
    protected virtual void StartFlowThrough(bool trap)
    {
        ProcessStep.Start($"Start flowing O2 through {InletPort.Name}");

        var gasfm = FTG_TFFlowManager;
        // Need to manage FTG gas source valve manually,
        // because we want the shutoff to be
        // downstream of the flow valve.
        var o2 = Find<IValve>("vO2_FTG");
        var destination = trap ? IM_FirstTrap : IM;

        var section = FTG_IP1;

        ProcessStep.Start($"Isolate and open {section.Name}");
        section.Isolate();
        section.Open();
        ProcessStep.End();

        // prepare upstream
        gasfm.FlowValve.CloseWait();

        // prepare downstream
        var vacfm = TFFlowManager;
        vacfm?.FlowValve.CloseWait();
        Find<IValve>("vTFBypass")?.CloseWait();

        // join everything
        if (trap)
        {
            StartCollecting();
            o2.OpenWait();
        }
        else
        {
            destination.OpenAndEvacuate(OkPressure);
            destination.Isolate();
            o2.OpenWait();
            InletPort.Open();
            destination.Evacuate();
        }

        // regulate the gas flow to maintain pressure
        var pressure = TF.Pressure;
        gasfm.Start(pressure);
        gasfm.Stop();
        vacfm?.Start(pressure);

        ProcessStep.End();   
    }

    /// <summary>
    /// Stop flowing O2 into the Inlet Port.
    /// </summary>
    protected virtual void StopFlowThroughGas()
    {
        ProcessStep.Start($"Stopping O2 flow into {InletPort.Name}");

        var gasfm = FTG_TFFlowManager;
        var vacfm = TFFlowManager;
        var supplyValve = Find<IValve>("vO2_FTG");
        var gasSupply = GasSupply("O2", TF);

        gasfm.Stop();
        gasfm.FlowValve.CloseWait();
        vacfm?.Stop();
        vacfm?.FlowValve.CloseWait();
        gasSupply.ShutOff();
        supplyValve.CloseWait();

        ProcessStep.End();
    }

    /// <summary>
    /// Open the TF outlet valves, joining TF to IP1
    /// </summary>
    protected virtual void OpenTF_IP1() => TF_IP1.Open();

    /// <summary>
    /// In situ quartz sample process, Day 1 (preparation)
    /// </summary>
    protected virtual void Day1()
    {
        TurnOnIpQuartzFurnace();
        BackfillTF1WithHe();
        LoadTF();
        SetParameter("IpEvacuationPressure", 1E-2);
        EvacuateIP();
        SetParameter("TfPressureTarget", 50);
        AdmitO2toTF();
        SetParameter("IpSetpoint", 100);
        TurnOnIpSampleFurnace();
        WaitIpRiseToSetpoint();
        StartFlowThroughToWaste();
        SetParameter("WaitTimerMinutes", 2);
        WaitForTimer();
        StopFlowThroughGas();
        EvacuateIP();
        AdmitO2toTF();
        StartFlowThroughToWaste();
        SetParameter("IpSetpoint", 95);
        TurnOffIpSampleFurnace();
        WaitIpFallToSetpoint();
        StopFlowThroughGas();
        TurnOffIpQuartzFurnace();
        OpenTF_VS1();
    }

    /// <summary>
    /// In situ quartz sample process, Day 2 (extraction)
    /// </summary>
    protected virtual void Day2()
    {
        TurnOnIpQuartzFurnace();
        BackfillTF1WithHe();
        LoadTF();
        EvacuateIP();
        SetParameter("TfPressureTarget", 50);
        AdmitO2toTF();
        SetParameter("IpSetpoint", 75);
        TurnOnIpSampleFurnace();
        WaitIpRiseToSetpoint();
        StartFlowThroughToWaste();
        SetParameter("WaitTimerMinutes", 1);
        WaitForTimer();
        StopFlowThroughGas();
        EvacuateIP();
        AdmitO2toTF();
        SetParameter("IpSetpoint", 150);
        AdjustIpSetpoint();
        WaitIpRiseToSetpoint();
        SetParameter("WaitTimerMinutes", 1);
        WaitForTimer();
        TurnOffIpSampleFurnace();
        SetParameter("FirstTrapBleedPressure", 10);
        StartFlowThroughToTrap();
        SetParameter("CollectUntilTemperatureFalls", 80);
        CollectUntilConditionMet();
        StopFlowThroughGas();
        OpenTF_IP1();
        ClearCollectionConditions();
        SetParameter("CollectUntilCtPressureFalls", 4.0);
        CollectUntilConditionMet();
        StopCollecting();
        OpenLine();
    }


    /// <summary>
    /// General-purpose code tester. Put whatever you want here.
    /// </summary>
    protected void Bakeout()
    {
        EvacuateIP();
        SetParameter("TfPressureTarget", 50);
        AdmitO2toTF();
        SetParameter("IpSetpoint", 500);
        TurnOnIpSampleFurnace();
        WaitIpRiseToSetpoint();
        StartFlowThroughToWaste();
        SetParameter("WaitTimerMinutes", 10);
        WaitForTimer();
        StopFlowThroughGas();
        EvacuateIP();
        AdmitO2toTF();
        SetParameter("IpSetpoint", 1000);
        AdjustIpSetpoint();
        WaitIpRiseToSetpoint();
        SetParameter("WaitTimerMinutes", 2);
        WaitForTimer();
        TurnOffIpSampleFurnace();
        OpenLine();
    }


    #endregion Process Steps


    #endregion Process Management

    #region Test functions

    protected override void Test()
    {
    }

    #endregion Test functions

}