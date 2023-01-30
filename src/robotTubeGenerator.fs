FeatureScript 1948;
// import(path : "onshape/std/geometry.fs", version : "1948.0");

// import(path : "3d660c69e8b171b47e5e5e49", version : "000000000000000000000000");

// const tubeNode = lookupNode("competition", "Competition", "FRC", [
//             lookupPayload("FRC", {}, "frcTable"),
//             // lookupPayload("FTC", {}, "ftcTable"),
//             lookupPayload("VEX", {}, "vexTable")
//         ]);

// const frcNode = lookupNode("system", "System", "Custom", [
//             lookupPayload("Custom", { "system" : "System.CUSTOM" }, "customTable"),
//             lookupPayload("VersaFrame", { "system" : "System.VERSA_FRAME" }, "versaFrameTable"),
//             lookupPayload("MAXTube", { "system" : "System.MAX_TUBE" }, "maxTubeTable")
//         ]);

// const customTableNode = lookupNode("type", "Type", "Tube", [
//             lookupPayload("Tube", {}, "customTubeTable"),
//             lookupPayload("Angle", {}, "customAngleTable"),
//             lookupPayload("Circular tube", {}, "customCircularTubeTable")
//         ]);

// // const ftcNode = lookupNode("type", "Type", "C channel", [
// //             lookupPayload("C channel", {}),
// //             lookupPayload("U channel", {}),
// //             lookupPayload("8020", {})
// //         ]);

// const vexNode = lookupNode("size", "Size", "1x2x1", [
//             lookupPayload("1x1", { "vexSize" : "VexSize.ONE_BY_ONE", "holeDiameter" : "\"0.177 in\"" }),
//             lookupPayload("1x2x1", { "vexSize" : "VexSize.ONE_BY_ONE", "holeDiameter" : "\"0.177 in\"" }),
//             lookupPayload("1x3x1", { "vexSize" : "VexSize.ONE_BY_ONE", "holeDiameter" : "\"0.177 in\"" }),
//             lookupPayload("1x5x1", { "vexSize" : "VexSize.ONE_BY_ONE", "holeDiameter" : "\"0.177 in\"" })
//         ]);

// const maxTubeNode = lookupNode("size", "Size", "2x1 light", [
//             lookupPayload("1x1", { "maxTubeSize" : "MaxTubeSize.ONE_BY_ONE" }),
//             lookupPayload("2x1 light", { "maxTubeSize" : "MaxTubeSize.TWO_BY_ONE_LIGHT" },
//             lookupNode("pattern", "Pattern", "Grid", [
//                         lookupPayload("None", { "maxTubePattern" : "MaxTubePattern.NONE" }),
//                         lookupPayload("Grid", { "maxTubePattern" : "MaxTubePattern.GRID" }),
//                         lookupPayload("Custom", { "maxTubePattern" : "MaxTubePattern.CUSTOM" }, maxTwoInchFacePattern)
//                     ])),
//             lookupPayload("2x1", { "maxTubeSize" : "MaxTubeSize.TWO_BY_ONE" },
//             lookupNode("pattern", "Pattern", "Custom", [
//                         lookupPayload("None", { "maxTubePattern" : "MaxTubePattern.NONE" }),
//                         lookupPayload("MAX", { "maxTubePattern" : "MaxTubePattern.MAX" }),
//                         lookupPayload("Custom", { "maxTubePattern" : "MaxTubePattern.CUSTOM" }, maxTwoInchFacePattern)
//                     ]))
//         ]);

// const maxTwoInchFacePattern = lookupNode("twoInchPattern", "2\\\" pattern", "Three rows", [
//                 lookupPayload("Center row", { "row2" : 1 }),
//                 lookupPayload("Two rows", { "row2" : 2 }),
//                 lookupPayload("Three rows", { "row2" : 3 })
//             ])->setNextNode(fixedSpacing);

// const fixedSpacing = lookupNode("spacing", "Spacing", "0.5", [
//                 lookupPayload("0.5", { "spacing" : "\"0.5 in\"" }),
//                 lookupPayload("1", { "spacing" : "\"1 in\"" })
//             ])->setNextNode(numberTenFit);

// const versaFrameNode = lookupNode("type", "Type", "Aluminum tube", [
//             lookupPayload("Aluminum tube", {},
//             lookupNode("size", "Size", "2x1", [
//                         lookupPayload("1x1", { "width" : "\"1 in\"", "height" : "\"1 in\"" },
//                         lookupNode("thickness", "Thickness", "0.100", [
//                                     lookupPayload("0.040", { "thickness" : "\"0.040 in\"" }),
//                                     lookupPayload("0.100", { "thickness" : "\"0.100 in\"" })
//                                 ])),
//                         lookupPayload("2x1", { "width" : "\"1 in\"", "height" : "\"2 in\"" },
//                         lookupNode("thickness", "Thickness", "0.100", [
//                                     lookupPayload("0.050", { "thickness" : "\"0.050 in\"" }),
//                                     lookupPayload("0.100", { "thickness" : "\"0.100 in\"" })
//                                 ]))
//                     ])),
//             lookupPayload("Plastic tube", {},
//             lookupNode("size", "Size", "2x1", [
//                         lookupPayload("1x1", { "width" : "\"1 in\"", "height" : "\"1 in\"", "thickness" : "\"0.100 in\"" }),
//                         lookupPayload("2x1", { "width" : "\"1 in\"", "height" : "\"2 in\"", "thickness" : "\"0.100 in\"" })
//                     ])),
//             lookupPayload("C channel", {},
//             lookupNode("size", "Size", "1x2x1", [
//                         lookupPayload("1x1x1", { "width" : "\"1 in\"", "height" : "\"1 in\"", "thickness" : "\"0.090 in\"" }),
//                         lookupPayload("1x2x1", { "width" : "\"1 in\"", "height" : "\"2 in\"", "thickness" : "\"0.090 in\"" })
//                     ])),
//             lookupPayload("Angle", {},
//             lookupNode("size", "Size", "1x1", [
//                         lookupPayload("1x1", { "width" : "\"1 in\"", "height" : "\"1 in\"", "thickness" : "\"0.090 in\"" }),
//                         lookupPayload("2x2", { "width" : "\"2 in\"", "height" : "\"2 in\"", "thickness" : "\"0.090 in\"" })
//                     ]))
//         ]);

// annotation { "Feature Type Name" : "Generate lookup table" }
// export const generateLookupTable = defineFeature(function(context is Context, id is Id, definition is map)
//     precondition
//     {
//     }
//     {
//         printTableExport("tubeTable", tubeNode);

//         printTable("frcTable", frcNode);

//         printTable("customTable", customTableNode);

//         printTable("versaFrameTable", versaFrameNode);

//         printTable("maxTubeTable", maxTubeNode);

//         // printTable("ftcTable", ftcNode);

//         printTable("vexTable", vexNode);

//         var customTube = customTubeNode;
//         customTube.entries[0].next = thicknessNode->setNextNode(oneByOneTubePattern());
//         customTube.entries[1].next = thicknessNode->setNextNode(twoByOneTubePattern());
//         customTube.entries[2].next = thicknessNode->setNextNode(customFacePattern());

//         printTable("customTubeTable", customTube);

//         var customAngle = customAngleNode;
//         customAngle.entries[0].next = thicknessNode->setNextNode(oneByOneTubePattern());
//         customAngle.entries[1].next = thicknessNode->setNextNode(customFacePattern());
//         customAngle.entries[2].next = thicknessNode->setNextNode(customFacePattern());
//         customAngle.entries[3].next = thicknessNode->setNextNode(customFacePattern());

//         printTable("customAngleTable", customAngle);

//         printTable("customCircularTubeTable", customCircularTubeNode);
//     });

// const customTubeNode = lookupNode("size", "Size", "2x1", [
//             lookupPayload("1x1", { "width" : "\"1 in\"", "height" : "\"1 in\"" }),
//             lookupPayload("2x1", { "width" : "\"1 in\"", "height" : "\"2 in\"" }),
//             lookupPayload("Custom", { "showWidth" : true, "showHeight" : true })
//         ]);

// const customAngleNode = lookupNode("size", "Size", "1x1", [
//                 lookupPayload("1x1", { "width" : "\"1 in\"", "height" : "\"1 in\"" }),
//                 lookupPayload("2x2", { "width" : "\"2 in\"", "height" : "\"2 in\"" }),
//                 lookupPayload("Custom", { "showWidth" : true, "showHeight" : true })
//             ])->setNextNode(thicknessNode);

// const thicknessNode = lookupNode("thickness", "Thickness", "1/16", [
//             lookupPayload("1/16", { "thickness" : "\"1/16 in\"" }),
//             lookupPayload("1/8", { "thickness" : "\"1/8 in\"" }),
//             lookupPayload("Custom", { "showThickness" : true })
//         ]);

// // circular tube
// const customCircularTubeNode = lookupNode("size", "Size", "0.5", [
//                 lookupPayload("0.5", { "diameter" : "\"0.5 in\"" }),
//                 lookupPayload("1", { "diameter" : "\"1 in\"" }),
//                 lookupPayload("Custom", { "showDiameter" : true })
//             ])->setNextNode(circularThicknessNode);

// const circularThicknessNode = lookupNode("thickness", "Thickness", "1/16", [
//             lookupPayload("1/16", { "thickness" : "\"1/16 in\"" }),
//             lookupPayload("1/8", { "thickness" : "\"1/8 in\"" }),
//             lookupPayload("Custom", { "showThickness" : true })
//         ]);

// // Hole pattern tables
// const customFacePattern1 = lookupNode("customFacePattern1", "Pattern", "Center row", [
//             lookupPayload("None", { "row1" : 0 }),
//             lookupPayload("Center row", { "row1" : 1, "showPatternLocation" : true }),
//             lookupPayload("Two rows", { "row1" : 2, "showPatternLocation" : true }),
//             lookupPayload("Three rows", { "row1" : 3, "showPatternLocation" : true }),
//             lookupPayload("Custom", { "showRow1" : true, "showPatternLocation" : true })
//         ]);

// const customFacePattern2 = lookupNode("customFacePattern2", "Pattern", "Center row", [
//             lookupPayload("None", { "row2" : 0 }),
//             lookupPayload("Center row", { "row2" : 1, "showPatternLocation" : true }),
//             lookupPayload("Two rows", { "row2" : 2, "showPatternLocation" : true }),
//             lookupPayload("Three rows", { "row2" : 3, "showPatternLocation" : true }),
//             lookupPayload("Custom", { "showRow2" : true, "showPatternLocation" : true })
//         ]);

// const smallCustomRowGap1 = lookupNode("rowGap1", "Row gap", "0.5", [
//             lookupPayload("0.5", { "rowGap1" : "\"0.5 in\"" }),
//             lookupPayload("0.75", { "rowGap1" : "\"0.75 in\"" }),
//             lookupPayload("Custom", { "showRowGap1" : true })
//         ]);

// const smallCustomRowGap2 = lookupNode("rowGap2", "Row gap", "0.5", [
//             lookupPayload("0.5", { "rowGap2" : "\"0.5 in\"" }),
//             lookupPayload("0.75", { "rowGap2" : "\"0.75 in\"" }),
//             lookupPayload("Custom", { "showRowGap2" : true })
//         ]);

// const largeCustomRowGap1 = lookupNode("rowGap1", "Row gap", "1", [
//             lookupPayload("1", { "rowGap1" : "\"1 in\"" }),
//             lookupPayload("1.5", { "rowGap1" : "\"1.5 in\"" }),
//             lookupPayload("Custom", { "showRowGap1" : true })
//         ]);

// const largeCustomRowGap2 = lookupNode("rowGap2", "Row gap", "1", [
//             lookupPayload("1", { "rowGap2" : "\"1 in\"" }),
//             lookupPayload("1.5", { "rowGap2" : "\"1.5 in\"" }),
//             lookupPayload("Custom", { "showRowGap2" : true })
//         ]);

// function customFacePattern() returns LookupNode
// {
//     var zeroPattern = customFacePattern2;
//     // 0 is terminal node
//     zeroPattern.entries[1].next = spacing;
//     zeroPattern.entries[2].next = largeCustomRowGap2->setNextNode(spacing);
//     zeroPattern.entries[3].next = smallCustomRowGap2->setNextNode(spacing);
//     zeroPattern.entries[4].next = smallCustomRowGap2->setNextNode(spacing);

//     var pattern2 = customFacePattern2;
//     pattern2.entries[0].next = spacing;
//     pattern2.entries[1].next = spacing;
//     pattern2.entries[2].next = largeCustomRowGap2->setNextNode(spacing);
//     pattern2.entries[3].next = smallCustomRowGap2->setNextNode(spacing);
//     pattern2.entries[4].next = smallCustomRowGap2->setNextNode(spacing);
//     for (var i = 1; i < size(pattern2.entries); i += 1)
//     {
//         pattern2.entries[i].value = mergeMaps(pattern2.entries[i].value, { "showOffset" : true });
//     }

//     var pattern1 = customFacePattern1;
//     pattern1.entries[0].next = zeroPattern;
//     pattern1.entries[1].next = pattern2;
//     pattern1.entries[2].next = largeCustomRowGap1->setNextNode(pattern2);
//     pattern1.entries[3].next = smallCustomRowGap1->setNextNode(pattern2);
//     pattern1.entries[4].next = smallCustomRowGap1->setNextNode(pattern2);
//     return pattern1;
// }

// const oneInchFacePattern = lookupNode("oneInchPattern", "1\\\" pattern", "Center row", [
//             lookupPayload("None", { "row1" : 0 }),
//             lookupPayload("Center row", { "row1" : 1, "showPatternLocation" : true }),
//             lookupPayload("Two rows", { "row1" : 2, "showPatternLocation" : true })
//         ]);

// const twoInchFacePattern = lookupNode("twoInchPattern", "2\\\" pattern", "Three rows", [
//             lookupPayload("None", { "row2" : 0 }),
//             lookupPayload("Center row", { "row2" : 1, "showPatternLocation" : true }),
//             lookupPayload("Two rows", { "row2" : 2, "showPatternLocation" : true }),
//             lookupPayload("Three rows", { "row2" : 3, "showPatternLocation" : true })
//         ]);

// function oneByOneTubePattern() returns LookupNode
// {
//     var oneInch = oneInchFacePattern;
//     for (var i = 1; i < size(oneInch.entries); i += 1)
//     {
//         oneInch.entries[i].next = spacing;
//         oneInch.entries[i].value = mergeMaps(oneInch.entries[i].value, { "showOffset" : true });
//     }
//     oneInch.entries[2].next = setNextNode(oneByOneTubeRowGap, spacing);
//     return oneInch;
// }

// const oneByOneTubeRowGap = lookupNode("rowGap1", "Row gap", "0.5", [
//             lookupPayload("0.5", { "rowGap1" : "\"0.5 in\"" }),
//             lookupPayload("Custom", { "showRowGap1" : true })
//         ]);

// function twoByOneTubePattern() returns LookupNode
// {
//     var twoInch = twoInchFacePattern;
//     twoInch.entries[0].next = spacing;
//     twoInch.entries[1].next = spacing;
//     twoInch.entries[2].next = smallRowGapAndSpacing;
//     twoInch.entries[3].next = largeRowGapAndSpacing;

//     // showOffset should only be true for non-zero values of the non-zero twoInch0 pattern
//     for (var i = 1; i < size(twoInch.entries); i += 1)
//     {
//         twoInch.entries[i].value = mergeMaps(twoInch.entries[i].value, { "showOffset" : true });
//     }

//     var twoInch0 = twoInchFacePattern;
//     // 0 is terminal node
//     twoInch0.entries[1].next = spacing;
//     twoInch0.entries[2].next = largeRowGapAndSpacing;
//     twoInch0.entries[3].next = smallRowGapAndSpacing;

//     var oneInch = oneInchFacePattern;
//     oneInch = setNextNode(oneInch, twoInch);
//     oneInch.entries[0].next = twoInch0;
//     oneInch.entries[1].next = twoInch;
//     oneInch.entries[2].next = setNextNode(oneByOneTubeRowGap, twoInch);

//     return oneInch;
// }

// const largeRowGap = lookupNode("rowGap2", "Row gap", "1", [
//             lookupPayload("1", { "rowGap2" : "\"1 in\"" }),
//             lookupPayload("1.5", { "rowGap2" : "\"1.5 in\"" }),
//             lookupPayload("Custom", { "showRowGap2" : true })
//         ]);

// const smallRowGap = lookupNode("rowGap2", "Row gap", "0.5", [
//             lookupPayload("0.5", { "rowGap2" : "\"0.5 in\"" }),
//             lookupPayload("0.75", { "rowGap2" : "\"0.75 in\"" }),
//             lookupPayload("Custom", { "showRowGap2" : true })
//         ]);

// const largeRowGapAndSpacing = setNextNode(largeRowGap, spacing);
// const smallRowGapAndSpacing = setNextNode(smallRowGap, spacing);

// const spacing = setNextNode(
//     lookupNode("spacing", "Spacing", "0.5", [
//                 lookupPayload("0.5", { "spacing" : "\"0.5 in\"" }),
//                 lookupPayload("1", { "spacing" : "\"1 in\"" }),
//                 lookupPayload("Custom", { "showSpacing" : true })
//             ]),
//     holeSize);

// // Hole size is always terminal
// const holeSize = lookupNode("holeSize", "Hole size", "#10", [
//             lookupPayload("#8", {}, numberEightFit),
//             lookupPayload("#10", {}, numberTenFit),
//             lookupPayload("Custom", { "showHoleDiameter" : true })
//         ]);

// const numberEightFit = lookupNode("fit", "Fit", "Free", [
//             lookupPayload("Close", { "holeDiameter" : "\"0.1695 in\"" }),
//             lookupPayload("Free", { "holeDiameter" : "\"0.177 in\"" })
//         ]);

// const numberTenFit = lookupNode("fit", "Fit", "Free", [
//             lookupPayload("Close", { "holeDiameter" : "\"0.196 in\"" }),
//             lookupPayload("Free", { "holeDiameter" : "\"0.201 in\"" })
//         ]);

