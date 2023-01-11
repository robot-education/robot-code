FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

export const BELT_TEETH_BOUNDS = { (unitless) : [1, 100, 1e50] } as IntegerBoundSpec;
export const OFFSET_BOUNDS = { (inch) : [-1e50, 0, 1e50] } as LengthBoundSpec;
export const CENTER_TO_CENTER_ADJUST_BOUNDS = { (inch) : [-0.50, 0, 0.50] } as LengthBoundSpec;

export const FIT_ADJUSTMENT_BOUNDS = { (inch) : [-0.05, 0, 0.05] } as LengthBoundSpec;

export const PULLEY_TEETH_BOUNDS = { (unitless) : [3, 24, 1e50] } as IntegerBoundSpec;

export const defaultGT2PulleyWidth = 0.4375 * inch;
export const default9mmHTDPulleyWidth = 0.4375 * inch;
export const default15mmHTDPulleyWidth = 0.75 * inch;

export enum BeltType
{
    annotation { "Name" : "GT2" }
    GT2,
    annotation { "Name" : "HTD" }
    HTD
}

export enum BeltWidth
{
    annotation { "Name" : "9mm" }
    _9MM,
    annotation { "Name" : "15mm" }
    _15MM
}

// GT2 Belts
export enum Gt2BeltSupplier
{
    annotation { "Name" : "VEXpro" }
    VEXPRO,
    annotation { "Name" : "REV" }
    REV,
    annotation { "Name" : "Custom" }
    CUSTOM
}

export enum VexproGt2Belt
{
    annotation { "Name" : "45T" }
    _45,
    annotation { "Name" : "50T" }
    _50,
    annotation { "Name" : "55T" }
    _55,
    annotation { "Name" : "60T" }
    _60,
    annotation { "Name" : "70T" }
    _70,
    annotation { "Name" : "85T" }
    _85,
    annotation { "Name" : "90T" }
    _90,
    annotation { "Name" : "100T" }
    _100,
    annotation { "Name" : "105T" }
    _105,
    annotation { "Name" : "110T" }
    _110,
    annotation { "Name" : "115T" }
    _115,
    annotation { "Name" : "120T" }
    _120,
    annotation { "Name" : "125T" }
    _125,
    annotation { "Name" : "140T" }
    _140,
    annotation { "Name" : "180T" }
    _180
}

export enum RevGt2Belt
{
    annotation { "Name" : "55T" }
    _55,
    annotation { "Name" : "85T" }
    _85,
    annotation { "Name" : "105T" }
    _105,
    annotation { "Name" : "120T" }
    _120,
    annotation { "Name" : "145T" }
    _145,
    annotation { "Name" : "210T" }
    _210,
    annotation { "Name" : "270T" }
    _270
}

// 9mm wide belts
export enum Htd9mmBeltSupplier
{
    annotation { "Name" : "VEXpro" }
    VEXPRO,
    annotation { "Name" : "AndyMark" }
    ANDYMARK,
    annotation { "Name" : "Custom" }
    CUSTOM
}

// 15mm wide belts
export enum Htd15mmBeltSupplier
{
    annotation { "Name" : "VEXpro" }
    VEXPRO,
    annotation { "Name" : "AndyMark" }
    ANDYMARK,
    annotation { "Name" : "Custom" }
    CUSTOM
}

// Same for 9mm and 15mm
export enum VexproHtdBelt
{
    annotation { "Name" : "60T" }
    _60,
    annotation { "Name" : "70T" }
    _70,
    annotation { "Name" : "80T" }
    _80,
    annotation { "Name" : "90T" }
    _90,
    annotation { "Name" : "100T" }
    _100,
    annotation { "Name" : "104T" }
    _104,
    annotation { "Name" : "110T" }
    _110,
    annotation { "Name" : "120T" }
    _120,
    annotation { "Name" : "130T" }
    _130,
    annotation { "Name" : "140T" }
    _140,
    annotation { "Name" : "150T" }
    _150,
    annotation { "Name" : "160T" }
    _160,
    annotation { "Name" : "170T" }
    _170,
    annotation { "Name" : "180T" }
    _180,
    annotation { "Name" : "200T" }
    _200,
    annotation { "Name" : "225T" }
    _225,
    annotation { "Name" : "250T" }
    _250
}

export enum AndymarkHtd9mmBelt
{
    annotation { "Name" : "40T" }
    _40,
    annotation { "Name" : "45T" }
    _45,
    annotation { "Name" : "48T" }
    _48,
    annotation { "Name" : "93T" }
    _93
}

// 55, 80, 85, 104, 107, 110, 117, 120, 131, 140, 151, 160, 170, 180, 200
export enum AndymarkHtd15mmBelt
{
    annotation { "Name" : "55T" }
    _55,
    annotation { "Name" : "80T" }
    _80,
    annotation { "Name" : "85T" }
    _85,
    annotation { "Name" : "104T" }
    _104,
    annotation { "Name" : "107T" }
    _107,
    annotation { "Name" : "110T" }
    _110,
    annotation { "Name" : "117T" }
    _117,
    annotation { "Name" : "120T" }
    _120,
    annotation { "Name" : "131T" }
    _131,
    annotation { "Name" : "140T" }
    _140,
    annotation { "Name" : "151T" }
    _151,
    annotation { "Name" : "160T" }
    _160,
    annotation { "Name" : "170T" }
    _170,
    annotation { "Name" : "180T" }
    _180,
    annotation { "Name" : "200T" }
    _200
}
