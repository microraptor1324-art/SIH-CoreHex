using UnityEngine;
using ARMiningSimulator.Decisions;
using ARMiningSimulator.Extinguisher;
using ARMiningSimulator.Investigation;
using ARMiningSimulator.Player;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Evacuation;

namespace ARMiningSimulator.Audio
{
    /// <summary>
    /// Plays the pre-recorded stage narration clips (Decision 1/2/3, Tactical Action Protocol,
    /// fire-origin identification) automatically when their corresponding stage actually becomes
    /// active. Listens to the existing stage-change events already fired by DecisionManager,
    /// FireResponseManager, and InvestigationManager — no changes to any of that gameplay logic.
    /// Plays through a single AudioSource so voice-overs never overlap, and never restarts a clip
    /// that's already playing (so a stage event firing again, e.g. re-opening a question after a
    /// wrong answer, doesn't stutter/restart the same line mid-sentence).
    /// </summary>
    public class VoiceOverManager : MonoBehaviour
    {
        public static VoiceOverManager Instance { get; private set; }

        [Header("Voice-Over Clips (assign the existing MP3 files here)")]
        [SerializeField] private AudioClip _fireSpottedClip;
        [SerializeField] private AudioClip _escapeClip;
        [SerializeField] private AudioClip _holdToExtinguishClip;
        [SerializeField] private AudioClip _allFiresSuppressedClip;
        [SerializeField] private AudioClip _yourScoreClip;
        [SerializeField] private AudioClip _decision1Clip;
        [SerializeField] private AudioClip _decision2Clip;
        [SerializeField] private AudioClip _decision3Clip;
        [SerializeField] private AudioClip _tacticalActionProtocolClip;
        [SerializeField] private AudioClip _identifyClip;

        [Header("Answer Feedback Clips")]
        [SerializeField] private AudioClip _correctAnswerClip;
        [SerializeField] private AudioClip _wrongAnswerClip;

        private AudioSource _source;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.loop = false;
            _source.playOnAwake = false;
        }

        private void OnEnable()
        {
            TraineeDetection.OnFireDetected += HandleFireDetected;
            EvacuationManager.OnEvacuationStarted += HandleEvacuationStarted;
            ExtinguisherController.OnExtinguisherEquipped += HandleExtinguisherEquipped;
            ExtinguisherController.OnAllActiveFiresExtinguished += HandleAllFiresSuppressed;
            DecisionManager.OnQuestionPresented += HandleQuestionPresented;
            DecisionManager.OnDecisionSubmitted += HandleDecisionSubmitted;
            FireResponseManager.OnStateChanged += HandleFireResponseStateChanged;
            FireResponseManager.OnEvaluationFeedback += HandleFireResponseFeedback;
            InvestigationManager.OnInvestigationStarted += HandleInvestigationStarted;
            InvestigationManager.OnMachineSelectionEvaluated += HandleMachineSelectionEvaluated;
            InvestigationManager.OnScorecardReady += HandleScorecardReady;
        }

        private void OnDisable()
        {
            TraineeDetection.OnFireDetected -= HandleFireDetected;
            EvacuationManager.OnEvacuationStarted -= HandleEvacuationStarted;
            ExtinguisherController.OnExtinguisherEquipped -= HandleExtinguisherEquipped;
            ExtinguisherController.OnAllActiveFiresExtinguished -= HandleAllFiresSuppressed;
            DecisionManager.OnQuestionPresented -= HandleQuestionPresented;
            DecisionManager.OnDecisionSubmitted -= HandleDecisionSubmitted;
            FireResponseManager.OnStateChanged -= HandleFireResponseStateChanged;
            FireResponseManager.OnEvaluationFeedback -= HandleFireResponseFeedback;
            InvestigationManager.OnInvestigationStarted -= HandleInvestigationStarted;
            InvestigationManager.OnMachineSelectionEvaluated -= HandleMachineSelectionEvaluated;
            InvestigationManager.OnScorecardReady -= HandleScorecardReady;
        }

        private void HandleFireDetected(FireHazard hazard, float reactionTime)
        {
            Play(_fireSpottedClip);
        }

        private void HandleEvacuationStarted()
        {
            Play(_escapeClip);
        }

        private void HandleExtinguisherEquipped(ExtinguisherConfig config)
        {
            Play(_holdToExtinguishClip);
        }

        private void HandleAllFiresSuppressed()
        {
            Play(_allFiresSuppressedClip);
        }

        private void HandleScorecardReady(ScoreBreakdown breakdown)
        {
            Play(_yourScoreClip);
        }

        private void HandleQuestionPresented(DecisionQuestion question)
        {
            if (question == null) return;

            if (question.stage == DecisionStage.Stage1_Alarm) Play(_decision1Clip);
            else if (question.stage == DecisionStage.Stage2_Ventilation) Play(_decision2Clip);
        }

        private void HandleFireResponseStateChanged(FireResponseState state)
        {
            if (state == FireResponseState.Stage3A_FireSizeEval) Play(_decision3Clip);
            else if (state == FireResponseState.Stage3A_ActionChoice) Play(_tacticalActionProtocolClip);
        }

        private void HandleInvestigationStarted()
        {
            Play(_identifyClip);
        }

        // Answer feedback — fires for every decision/evaluation the player submits across the
        // whole scenario (Decision 1, Decision 2, fire-size eval, tactical action choice, fire
        // extinguisher pick, and fire-origin machine tap), regardless of which system owns it.
        private void HandleDecisionSubmitted(DecisionRecord record)
        {
            if (record == null) return;
            Play(record.wasCorrect ? _correctAnswerClip : _wrongAnswerClip);
        }

        private void HandleFireResponseFeedback(string text, bool isCorrect, int score)
        {
            Play(isCorrect ? _correctAnswerClip : _wrongAnswerClip);
        }

        private void HandleMachineSelectionEvaluated(bool isCorrect, string text)
        {
            Play(isCorrect ? _correctAnswerClip : _wrongAnswerClip);
        }

        /// <summary>Plays a clip, stopping whatever is currently playing first — never two at once —
        /// but does nothing if this exact clip is already the one playing.</summary>
        private void Play(AudioClip clip)
        {
            if (clip == null || _source == null) return;
            if (_source.isPlaying && _source.clip == clip) return;

            _source.Stop();
            _source.clip = clip;
            _source.Play();
        }
    }
}
