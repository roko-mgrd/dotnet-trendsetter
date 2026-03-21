namespace Trendsetter.Trends;

public static class SampleMedicalRecord
{
    public const string Text =
        """
        ══════════════════════════════════════════════════════════════
        MEDICAL RECORD — CONFIDENTIAL
        ══════════════════════════════════════════════════════════════

        PATIENT INFORMATION
        ───────────────────
        Name:           John Michael Doe
        Date of Birth:  1985-06-12
        Gender:         Male
        Member ID:      MBR-2024-98765

        INSURANCE INFORMATION
        ─────────────────────
        Payer:          Blue Cross Blue Shield
        Plan Type:      PPO
        Group Number:   GRP-445566

        ══════════════════════════════════════════════════════════════
        ENCOUNTER SUMMARY
        ══════════════════════════════════════════════════════════════

        PROCEDURE #1
        ────────────
        Date of Treatment:  2024-03-15
        Provider:           Dr. Sarah Smith
        Procedure Name:     Rotator cuff repair
        Description:        Surgical repair of torn rotator cuff tendon
                            using arthroscopic technique. The patient
                            presented with chronic right shoulder pain.
        Associated Diagnoses:
          - M75.110  Rotator cuff tear or rupture of right shoulder
          - M75.100  Rotator cuff syndrome of right shoulder

        PROCEDURE #2
        ────────────
        Date of Treatment:  2024-05-22
        Provider:           Dr. James Wilson
        Procedure Name:     Physical therapy evaluation
        Description:        Comprehensive PT evaluation for post-operative
                            rotator cuff rehabilitation. Range of motion
                            and strength assessment performed.
        Associated Diagnoses:
          - Z96.611  Presence of right artificial shoulder joint
          - M25.511  Pain in right shoulder

        ══════════════════════════════════════════════════════════════
        ALL DIAGNOSES (MASTER LIST)
        ══════════════════════════════════════════════════════════════
          1. M75.110  Rotator cuff tear or rupture of right shoulder
          2. M75.100  Rotator cuff syndrome of right shoulder
          3. Z96.611  Presence of right artificial shoulder joint
          4. M25.511  Pain in right shoulder
          5. E11.9    Type 2 diabetes mellitus without complications
          6. I10      Essential hypertension

        ══════════════════════════════════════════════════════════════
        END OF RECORD
        ══════════════════════════════════════════════════════════════
        """;
}
