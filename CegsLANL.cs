using System;
using System.ComponentModel;
using System.Linq;
using AeonHacs.Utilities;
using static AeonHacs.Components.CegsPreferences;
using static AeonHacs.Utilities.Utility;

namespace AeonHacs.Components
{
    public partial class CegsLANL : Cegs
    {
        #region HacsComponent

        [HacsPreConnect]
        protected virtual void PreConnect()
        {
            #region Logs

            SampleLog = Find<HacsLog>("SampleLog");
            TestLog = Find<HacsLog>("TestLog");

            VM1PressureLog = Find<DataLog>("VM1PressureLog");
            VM1PressureLog.Changed = (col) => col.Resolution > 0 && col.Source is Manometer m ?
                (col.PriorValue is double p ?
                    Manometer.SignificantChange(p, m.Pressure) :
                    true) :
                false;

            AmbientLog = Find<DataLog>("AmbientLog");
            // These components are needed to allow the inclusion of
            // non-INamedValue properties of theirs in logged data.
            HeaterController1 = Find<HC6Controller>("HeaterController1");
            HeaterController2 = Find<HC6Controller>("HeaterController2");
            AmbientLog.AddNewValue("HC1.CJ", -1, "0.0",
                () => HeaterController1.ColdJunctionTemperature);
            AmbientLog.AddNewValue("HC2.CJ", -1, "0.0",
                () => HeaterController2.ColdJunctionTemperature);

            #endregion Logs
        }

        [HacsConnect]
        protected override void Connect()
        {
            base.Connect();

            #region a Cegs needs these
            // The base Cegs really can't do "carbon extraction and graphitization"
            // unless these objects are defined.

            Power = Find<Power>("Power");
            Ambient = Find<Chamber>("Ambient");
            VacuumSystem1 = Find<VacuumSystem>("VacuumSystem1");

            IM = Find<Section>("IM");
            VTT = Find<Section>("VTT");
            MC = Find<Section>("MC");
            Split = Find<Section>("Split");
            GM = Find<Section>("GM");

            VTT_MC = Find<Section>("VTT_MC");
            MC_Split = Find<Section>("MC_Split");

            ugCinMC = Find<Meter>("ugCinMC");

            InletPorts = CachedList<IInletPort>();
            GraphiteReactors = CachedList<IGraphiteReactor>();
            d13CPorts = CachedList<Id13CPort>();

            #endregion a Cegs needs these

            CT = Find<Section>("CT");
            IM_CT = Find<Section>("IM_CT");
            CT_VTT = Find<Section>("CT_VTT");
            TF = Find<Section>("TF");
            d13C = Find<Section>("d13C");
            AM = Find<Section>("AM");
            d13CM = AM;
            FTG = Find<Section>("FTG");
            //IP1 = Find<Section>("IP1");
            //IP2 = Find<Section>("IP2");
            TF_IP1 = Find<Section>("TF_IP1");
            FTG_TF = Find<Section>("FTG_TF");
            FTG_IP1 = Find<Section>("FTG_IP1");
            MC_GM = Find<Section>("MC_GM");

            VS1All = Find<Section>("VS1All");

            TF1 = Find<TcpTubeFurnace>("TF1");
            IpOvenRamper = Find<OvenRamper>("IpOvenRamper");
            pAmbient = Find<AIManometer>("pAmbient");
            FTG_TFFlowManager = Find<FlowManager>("FTG_TFFlowManager");
            TFFlowManager = Find<FlowManager>("TFFlowManager");
        }
        #endregion HacsComponent

        #region System configuration
        #region HacsComponents
        public DataLog GRSTLog { get; set; }
        public GraphiteReactor GR1 { get; set; }
        public GraphiteReactor GR2 { get; set; }
        public GraphiteReactor GR3 { get; set; }
        public GraphiteReactor GR4 { get; set; }
        public GraphiteReactor GR5 { get; set; }
        public GraphiteReactor GR6 { get; set; }

        public HC6Controller HeaterController1 { get; set; }
        public HC6Controller HeaterController2 { get; set; }
        public HC6Controller HeaterController3 { get; set; }
        public HC6Controller HeaterController4 { get; set; }

        /// <summary>
        /// Tube furnace section
        /// </summary>
        public ISection TF { get; set; }

        /// <summary>
        /// Auxiliary Manifold section
        /// </summary>
        public ISection AM { get; set; }

        /// <summary>
        /// Flow Through Gas section
        /// </summary>
        public ISection FTG { get; set; }

        /// <summary>
        /// Inlet Port 1 section
        /// </summary>
        //public ISection IP1 { get; set; }

        /// <summary>
        /// Inlet Port 2 section
        /// </summary>
        //public ISection IP2 { get; set; }

        public virtual double umolCinMC => ugCinMC.Value / gramsCarbonPerMole;
        public virtual ISection IM_CT { get; set; }
        public virtual ISection CT_VTT { get; set; }
        public virtual ISection MC_GM { get; set; }
        /// <summary>
        /// Tube furnace..Inlet Port 1 section
        /// </summary>
        public ISection TF_IP1 { get; set; }

        /// <summary>
        /// Flow-Through Gas..Tube Furnace section
        /// </summary>
        public ISection FTG_TF { get; set; }

        /// <summary>
        /// Flow-Through Gas..Tube Furnace..Inlet Port 1 section
        /// </summary>
        public ISection FTG_IP1 { get; set; }


        /// <summary>
        /// All of the chambers evacuated by Vacuum System 1 (Inlets), except ports
        /// </summary>
        public ISection VS1All { get; set; }


        /// <summary>
        /// Tube furnace
        /// </summary>
        public TcpTubeFurnace TF1 { get; set; }


        /// <summary>
        /// Ramped temperature controller for Inlet Port
        /// </summary>
        public OvenRamper IpOvenRamper { get; set; }


        /// <summary>
        /// Ambient air pressure.
        /// </summary>
        public AIManometer pAmbient { get; set; }


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

        protected override void ZeroPressureGauges()
        {
            base.ZeroPressureGauges();

            // do not auto-zero pressure gauges while a process is running
            if (Busy) return;

            bool OkToZeroManometer(ISection section) =>
                    section is Section s &&
                    s.VacuumSystem.TimeAtBaseline.TotalSeconds > 20 &&
                    (s.PathToVacuum?.IsOpened() ?? false);

            if (OkToZeroManometer(MC))
                ZeroIfNeeded(MC?.Manometer, 5);

            if (OkToZeroManometer(CT))
                ZeroIfNeeded(CT?.Manometer, 5);

            if (OkToZeroManometer(IM))
                ZeroIfNeeded(IM?.Manometer, 2);

            if (OkToZeroManometer(GM))
            {
                ZeroIfNeeded(GM?.Manometer, 2);
                foreach (var gr in GraphiteReactors)
                    if (Manifold(gr).PathToVacuum.IsOpened() && gr.IsOpened)
                        ZeroIfNeeded(gr.Manometer, 2);
            }
        }

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
        protected virtual void CloseGasSupplies()
        {
            ProcessSubStep.Start("Close gas supplies");

            // Look only in CEGS vacuum systems; ignore other process managers
            var cegsGasSupplies = GasSupplies.Values.Where(gs => VacuumSystems.ContainsValue(gs.Destination.VacuumSystem)).ToList();
            cegsGasSupplies.ForEach(gs => gs.ShutOff());
            // close gas flow valves after all shutoff valves are closed
            cegsGasSupplies.ForEach(gs => gs.FlowValve?.CloseWait());

            ProcessSubStep.End();
        }

        /// <summary>
        /// Open and evacuate the entire vacuum line. This establishes
        /// the baseline system state: the condition it is normally left in
        /// when idle, and the expected starting point for major
        /// processes such as running samples.
        /// </summary>
        protected override void OpenLine() =>
            FastOpenLine();

        /// <summary>
        /// Open and evacuate the vacuum line quickly, without special
        /// attention to the sequence of chambers.
        /// </summary>
        protected virtual void FastOpenLine()
        {
            ProcessStep.Start("Close gas supplies");
            CloseGasSupplies();
            ProcessStep.End();

            OpenVS1Line();
            ProcessStep.End();

        }


        /// <summary>
        /// Open and evacuate the chambers normally serviced by VacuumSystem1
        /// </summary>
        protected virtual void OpenVS1Line()
        {
            ProcessStep.Start("Open VacuumSystem1 line");
            if (!VS1All.IsOpened || !VS1All.PathToVacuum.IsOpened())
                VS1All.OpenAndEvacuate();
            ProcessStep.End();
        }


        /// <summary>
        /// Open and evacuate the chambers normally serviced by VacuumSystem1 including the TF
        /// </summary>
        protected virtual void OpenTF_VS1()
        {
            ProcessStep.Start("Open and evacuate TF and VS1");
            TF.Evacuate(IpEvacuationPressure);
            TF_IP1.Open();
            Find<InletPort>("IP1").Open();
            OpenVS1Line();
        }


        #endregion OpenLine

        /// <summary>
        /// Whenever the MC sample measurement (in ugC) changes,
        /// notify subscribers that umolCinMC has changed as well.
        /// </summary>
        protected override void UpdateSampleMeasurement(object sender = null, PropertyChangedEventArgs e = null)
        {
	        var ugC = ugCinMC.Value;
	        base.UpdateSampleMeasurement(sender, e);
	        if (ugCinMC.Value != ugC)
	            NotifyPropertyChanged(nameof(umolCinMC));
        }

        #region Process Control Parameters

        /// <summary>
        /// Inlet Port sample furnace working setpoint ramp rate (degrees per minute).
        /// </summary>
        public double IpRampRate => GetParameter("IpRampRate");

        /// <summary>
        /// The Inlet Port sample furnace's target setpoint (the final setpoint when ramping).
        /// </summary>
        public double IpSetpoint => GetParameter("IpSetpoint");

        /// <summary>
        /// The desired Inlet Manifold pressure, used for filling or flow management.
        /// </summary>
        public double ImPressureTarget => GetParameter("ImPressureTarget");

        /// <summary>
        /// Tube furnace working setpoint ramp rate (degrees per minute).
        /// </summary>
        //public double TfRampRate => GetParameter("TfRampRate");

        /// <summary>
        /// The Tube Furnace's target setpoint (the final setpoint when ramping).
        /// </summary>
        //public double TfSetpoint => GetParameter("TfSetpoint");

        /// <summary>
        /// The desired Tube Furnace  pressure, used for filling or flow management.
        /// </summary>
        public double TfPressureTarget => GetParameter("TfPressureTarget");

        /// <summary>
        /// During sample collection, close the Inlet Port when the Inlet Manifold pressure falls to this value, 
        /// provided that it is a number (i.e., not NaN).
        /// </summary>
        public double CollectCloseIpAtPressure => GetParameter("CollectCloseIpAtPressure");

        /// <summary>
        /// During sample collection, close the Inlet Port when the Coil Trap pressure falls to this value,
        /// provided that it is a number (i.e., not NaN).
        /// </summary>
        public double CollectCloseIpAtCtPressure => GetParameter("CollectCloseIpAtCtPressure");

        /// <summary>
        /// Stop collecting into the coil trap when the Inlet Port temperature rises to this value,
        /// provided that it is a number (i.e., not NaN).
        /// </summary>
        public double CollectUntilTemperatureRises => GetParameter("CollectUntilTemperatureRises");

        /// <summary>
        /// Stop collecting into the coil trap when the Inlet Port temperature falls to this value,
        /// provided that it is a number (i.e., not NaN).
        /// </summary>
        public double CollectUntilTemperatureFalls => GetParameter("CollectUntilTemperatureFalls");

        /// <summary>
        /// Stop collecting when the Coil Trap pressure falls to or below this value,
        /// provided that it is a number (i.e., not NaN).
        /// </summary>
        public double CollectUntilCtPressureFalls => GetParameter("CollectUntilCtPressureFalls");

        /// <summary>
        /// Stop collecting into the coil trap when amount of carbon in the Coil Trap reaches this value,
        /// provided that it is a number (i.e., not NaN).
        /// </summary>
        public double CollectUntilUgc => GetParameter("CollectUntilUgc");

        /// <summary>
        /// Stop collecting into the coil trap when this much time has elapsed. 
        /// provided that the value is a number (i.e., not NaN).
        /// </summary>
        public double CollectUntilMinutes => GetParameter("CollectUntilMinutes");

        /// <summary>
        /// How many minutes to wait.
        /// </summary>
        public double WaitTimerMinutes => GetParameter("WaitTimerMinutes");

        /// <summary>
        /// What pressure to evacuate InletPort to.
        /// </summary>
        public double IpEvacuationPressure => GetParameter("IpEvacuationPressure");

        #endregion Process Control Parameters


        #region Process Control Properties

        public virtual bool IpIsTubeFurnace => InletPort.SampleFurnace is TubeFurnace;

        /// <summary>
        /// Change the Inlet Port Sample furnace setpoint at a controlled
        /// ramp rate, rather than immediately to the given value.
        /// </summary>
        public virtual bool EnableIpSetpointRamp { get; set; } = false;

        /// <summary>
        /// Change the Tube Furnace setpoint at a controlled
        /// ramp rate, rather than immediately to the given value.
        /// </summary>
        public virtual bool EnableTfSetpointRamp { get; set; } = false;

        /// <summary>
        /// Provide a flow of oxygen through the Inlet Port to combust the sample,
        /// instead of a fixed pressure.
        /// </summary>
        public virtual bool NeedIpFlow { get; set; } = false;

        /// <summary>
        /// Direct the sample gas through the CO2 analyzer during collection.
        /// </summary>
        public virtual bool NeedAnalyzer { get; set; } = false;

        /// <summary>
        /// Monitors the time elapsed since the current sample collection phase began.
        /// </summary>
        public Stopwatch CollectStopwatch { get; set; } = new Stopwatch();


        #endregion Process Control Properties


        #region Process Steps
        /// <summary>
        /// Wait for timer minutes.
        /// </summary>
        protected virtual void WaitForTimer()
        {
            ProcessStep.Start($"Wait for {WaitTimerMinutes:0} minutes");
            WaitFor(() => ProcessStep.Elapsed.TotalMinutes >= WaitTimerMinutes);
            ProcessStep.End();
        }
        

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
        /// Turn on the Inlet Port quartz furnace.
        /// </summary>
        protected virtual void TurnOnIpQuartzFurnace() => InletPort.QuartzFurnace.TurnOn();

        /// <summary>
        /// Turn off the Inlet Port quartz furnace.
        /// </summary>
        protected virtual void TurnOffIpQuartzFurnace() => InletPort.QuartzFurnace.TurnOff();

        /// <summary>
        /// Adjust the Inlet Port sample furnace setpoint. If its
        /// setpoint ramp is enabled, the working setpoint will be managed
        /// to reach the new setpoint at the programmed ramp rate.
        /// </summary>
        protected virtual void AdjustIpSetpoint()
        {
            if (IpSetpoint.IsNaN()) return;
            if (IpOvenRamper.Enabled)
                IpOvenRamper.Setpoint = IpSetpoint;
            else
                InletPort.SampleFurnace.Setpoint = IpSetpoint;
        }

        /// <summary>
        /// Wait for Inlet Port temperature to fall below IpSetpoint
        /// </summary>
        protected virtual void WaitIpFallToSetpoint()
        {
            AdjustIpSetpoint();
            bool shouldStop()
            {
                if (Stopping)
                    return true;
                if (InletPort.Temperature <= IpSetpoint)
                    return true;
                return false;
            }
            ProcessStep.Start($"Waiting for {InletPort.Name} to reach {IpSetpoint:0} °C");
            WaitFor(shouldStop, -1, 1000);
            ProcessStep.End();
        }

        /// <summary>
        /// Turn on the Inlet Port sample furnace.
        /// </summary>
        protected virtual void TurnOnIpSampleFurnace()
        {
            AdjustIpSetpoint();
            InletPort.SampleFurnace.TurnOn();
        }
        
        /// <summary>
        /// Wait for the InletPort sample furnace to reach the setpoint.
        /// </summary>
        protected virtual void WaitIpRiseToSetpoint()
        {
            bool shouldStop()
            {
                if (Stopping)
                    return true;
                if (InletPort.Temperature >= IpSetpoint)
                    return true;
                return false;
            }
            ProcessStep.Start($"Waiting for {InletPort.Name} to reach {IpSetpoint:0} °C");
            WaitFor(shouldStop, -1, 1000);
            ProcessStep.End();
        }

        /// <summary>
        /// Turn off the Inlet Port sample furnace.
        /// </summary>
        protected virtual void TurnOffIpSampleFurnace() => InletPort.SampleFurnace.TurnOff();

        /// <summary>
        /// Adjust the Inlet Port sample furnace ramp rate.
        /// </summary>
        protected virtual void AdjustIpRampRate() => IpOvenRamper.RateDegreesPerMinute = IpRampRate;

        /// <summary>
        /// Enable the Inlet Port sample furnace setpoint ramp.
        /// </summary>
        protected virtual void EnableIpRamp()
        {
            IpOvenRamper.Oven = InletPort.SampleFurnace;
            IpOvenRamper.Enable();
        }

        /// <summary>
        /// Disable the Inlet Port sample furnace setpoint ramp.
        /// </summary>
        protected virtual void DisableIpRamp() => IpOvenRamper.Disable();


        /// <summary>
        /// Fill the Tube Furnace chamber with helium in preparation
        /// for opening.
        /// </summary>
        protected virtual void BackfillTF1WithHe()
        {
            ProcessStep.Start($"Fill {InletPort.Name} to {pAmbient.Pressure:0} Torr He");
            
            // Need to manage FTG gas source valve manually,
            // because we want the shutoff to be
            // downstream of the flow valve.
            var he = Find<IValve>("vHe_FTG");

            FTG_IP1.Isolate();
            var gs = InertGasSupply(TF);
            he.OpenWait();
            gs.FlowPressurize(pAmbient.Pressure);
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
        /// ?Start flowing O2 through the Inlet Port to vacuum.?
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
            var destination = trap ? IM_CT : IM;

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
        /// Start collecting sample into a coil trap.
        /// </summary>
        protected virtual void StartCollecting() => StartCtFlow(true);

        protected virtual void StartCtFlow(bool freezeTrap)
        {
            var status = freezeTrap ?
                $"Start collecting sample in {CT.Name}" :
                $"Start gas flow through {CT.Name}"; 
            ProcessStep.Start(status);

            //ClearCollectionConditions();
            //IM_CT.OpenAndEvacuate(OkPressure);
            if (freezeTrap)
                CT.WaitForFrozen(false);
            CT.FlowValve.CloseWait();
            InletPort.Open();
            Sample.CoilTrap = CT.Name;
            InletPort.State = LinePort.States.InProcess;
            CollectStopwatch.Restart();
            CT.FlowManager.Start(FirstTrapBleedPressure);

            ProcessStep.End();
        }

        /// <summary>
        /// Set all collection condition parameters to NaN
        /// </summary>
        protected void ClearCollectionConditions()
        {
            ClearParameter("CollectUntilTemperatureRises");
            ClearParameter("CollectUntilTemperatureFalls");
            ClearParameter("CollectCloseIpPressure");
            ClearParameter("CollectUntilCtPressureFalls");
            ClearParameter("CollectUntilUgc");
            ClearParameter("CollectUntilMinutes");
        }

        string stoppedBecause = "";
        /// <summary>
        /// Wait for a collection stop condition to occur.
        /// </summary>
        protected virtual void CollectUntilConditionMet()
        {
            ProcessStep.Start($"Wait for a collection stop condition");

            bool shouldStop()
            {
                if (CollectStopwatch.IsRunning && CollectStopwatch.ElapsedMilliseconds < 1000)
                    return false;

                // TODO: what if flow manager becomes !Busy (because, e.g., FlowValve is fully open)?
                // TODO: should we invoke DuringBleed()? When?
                // TODO: should we disable/enable CT.VacuumSystem.Manometer?

                // Open flow bypass when conditions allow it without producing an excessive
                // downstream pressure spike. Then wait for the spike to be evacuated.
                if (IM.Pressure - FirstTrap.Pressure < FirstTrapFlowBypassPressure)
                    FirstTrap.Open();   // open bypass if available


                if (CollectCloseIpAtPressure.IsANumber() && InletPort.IsOpened && InletPort.Pressure <= CollectCloseIpAtPressure)
                    InletPort.Close();
                if (CollectCloseIpAtCtPressure.IsANumber() && InletPort.IsOpened && CT.Pressure <= CollectCloseIpAtCtPressure)
                    InletPort.Close();

                if (Stopping)
                {
                    stoppedBecause = "CEGS is shutting down";
                    return true;
                }
                if (CollectUntilTemperatureRises.IsANumber() && InletPort.Temperature >= CollectUntilTemperatureRises)
                {
                    stoppedBecause = $"InletPort.Temperature rose to {CollectUntilTemperatureRises:0} °C";
                    return true;
                }
                if (CollectUntilTemperatureFalls.IsANumber() && InletPort.Temperature <= CollectUntilTemperatureFalls)
                {
                    stoppedBecause = $"InletPort.Temperature fell to {CollectUntilTemperatureFalls:0} °C";
                    return true;
                }

                // old?: FirstTrap.Pressure < FirstTrapEndPressure;
                if (CollectUntilCtPressureFalls.IsANumber() && CT.Pressure <= CollectUntilCtPressureFalls && IM.Pressure < Math.Ceiling(CollectUntilCtPressureFalls)+2)
                {
                    stoppedBecause = $"CoilTrap.Pressure fell to {CollectUntilCtPressureFalls:0.00} Torr";
                    return true;
                }
                if (CollectUntilMinutes.IsANumber() && CollectStopwatch.Elapsed.TotalMinutes >= CollectUntilMinutes)
                {
                    stoppedBecause = $"{MinutesString((int) CollectUntilMinutes)} elapsed";
                    return true;
                }

                stoppedBecause = "";
                return false;
            }
            WaitFor(shouldStop, -1, 1000);
            SampleLog.Record($"{Sample.LabId}\tStopped collecting:\t{stoppedBecause}");

            ProcessStep.End();
        }

        /// <summary>
        /// Stop collecting immediately
        /// </summary>
        protected virtual void StopCollecting() => StopCollecting(true);

        /// <summary>
        /// Close the IP and wait for CT pressure to bleed down until it stops falling.
        /// </summary>
        protected virtual void StopCollectingAfterBleedDown() => StopCollecting(false);

        /// <summary>
        /// Stop collecting. If 'immediately' is false, wait for CT pressure to bleed down after closing IP
        /// </summary>
        /// <param name="immediately">If false, wait for CT pressure to bleed down after closing IP</param>
        protected virtual void StopCollecting(bool immediately = true)
        {
            ProcessStep.Start("Stop Collecting");

            CT.FlowManager?.Stop();
            InletPort.Close();
            if (!immediately) 
                FinishCollecting();
            IM_CT.Close();
            CT.Isolate();
            CT.FlowValve.CloseWait();

            ProcessStep.End();
        }

        /// <summary>
        /// Wait until pCT stops falling
        /// </summary>
        protected virtual void FinishCollecting()
        {
            ProcessStep.Start($"Wait for {IM_CT.Name} pressure to stop falling");
            WaitFor(() => !CT.Manometer.IsFalling);
            ProcessStep.End();
        }

        /// <summary>
        /// Override CEGS Collect() to use parameter-driven methods.
        /// TODO: refactor the base class code to adopt this approach.
        /// </summary>
        protected override void Collect()
        {
            VS1All.Isolate();
            IM_CT.Isolate();
            IM_CT.FlowValve.OpenWait();
            IM_CT.OpenAndEvacuate(OkPressure);

            StartCollecting();
            CollectUntilConditionMet();
            StopCollecting(false);
            InletPort.State = LinePort.States.Complete;

            TransferCO2FromCTToVTT();
        }


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

        protected void LeakCheckAllPorts()
        {
            var ports = FindAll<IPort>();
            ports.ForEach(port =>
            {
                if (port.Name != "MCP1" && port.Name != "MCP2" && port.Name != "DeadCO2")
                {
                    var rate = PortLeakRate(port);
                    SampleLog.Record($"{port.Name} leak rate: {rate:0.0e0} Torr L/s");
                }
            });
        }


        /// <summary>
        /// TODO: use a search function to make this system-independent
        /// and move it to the base class.
        /// </summary>
        protected void CalibrateManualHeaters()
        {
            var tc = Find<IThermocouple>("tCal");
            CalibrateManualHeater(Find<IHeater>("hIP1CCQ"), tc);
            CalibrateManualHeater(Find<IHeater>("hIP2CCQ"), tc);
            CalibrateManualHeater(Find<IHeater>("hIP3CCQ"), tc);
            CalibrateManualHeater(Find<IHeater>("hIP4CCQ"), tc);
            CalibrateManualHeater(Find<IHeater>("hIP5CCQ"), tc);
            CalibrateManualHeater(Find<IHeater>("hIP6CCQ"), tc);
        }



        protected override void Test()
        {
       }

        #endregion Test functions

    }
}