using UnityEngine;

public enum GamePhase { Tipp, Piramis, Busz, JatekVege }

public enum TippType { NONE, PirosVagyFekete, AlattaVagyFelette, KozteVagySzet, PontosTipus, PontosSzam }

public enum TippValue
{
    NONE,
    KETTO = 2, HAROM = 3, NEGY = 4, OT = 5, HAT = 6, HET = 7, NYOLC = 8, KILENC = 9, TIZ = 10, JACK = 11, QUEEN = 12, KING = 13, ACE = 14,
    PIROS, FEKETE,
    ALATTA, UGYANAZ, FELETTE,
    KOZTE, SZET, UGYANAZ_ALSO, UGYANAZ_FELSO,
    SZIV, ROMBUSZ, LOHERE, PIKK
}