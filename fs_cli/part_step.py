"""A built part (`fs_eval.geometry.BuiltPart`) as a STEP file: its parts (solids), each named as its `NAME` property
and colored as its appearance (a face with its own, like a nylock's insert, keeps it), in millimeters. Written with
OpenCascade's XDE, as `fs render --step` does. Mate connectors and cosmetic threads aren't STEP geometry, so they're
left out (the metadata `fs render` writes beside it lists them)."""

from __future__ import annotations

import pathlib

from fs_cli.part_mesh import _color


def write_step(built, path: pathlib.Path) -> None:
    """Writes the built part's parts to `path`."""
    from OCP.IFSelect import IFSelect_ReturnStatus
    from OCP.Interface import Interface_Static
    from OCP.STEPCAFControl import STEPCAFControl_Writer
    from OCP.STEPControl import STEPControl_AsIs
    from OCP.TCollection import TCollection_ExtendedString
    from OCP.TDataStd import TDataStd_Name
    from OCP.TDocStd import TDocStd_Document
    from OCP.XCAFDoc import XCAFDoc_ColorType, XCAFDoc_DocumentTool

    from fs_eval.geometry import fsvalues as fv

    parts = built.parts
    if not parts:
        raise ValueError("It made no parts")
    document = TDocStd_Document(TCollection_ExtendedString("XmlOcaf"))
    # The model is in meters, as Onshape's is; the file is in millimeters
    XCAFDoc_DocumentTool.SetLengthUnit_s(document, 1.0)
    shapes = XCAFDoc_DocumentTool.ShapeTool_s(document.Main())
    colors = XCAFDoc_DocumentTool.ColorTool_s(document.Main())
    model = built.model
    for body in parts:
        label = shapes.AddShape(body.shape, False)
        name = body.properties.get("NAME")
        if name is not None:
            TDataStd_Name.Set_s(label, TCollection_ExtendedString(str(fv.to_plain(name))))
        color = _color(body.properties.get("APPEARANCE"))
        if color is not None:
            colors.SetColor(label, _quantity(color), XCAFDoc_ColorType.XCAFDoc_ColorSurf)
        for tid in body.faces:
            face_color = _color(model.face_properties.get(tid, {}).get("APPEARANCE"))
            if face_color is not None and face_color != color:
                face = shapes.AddSubShape(label, model.entities[tid].shape)
                if not face.IsNull():
                    colors.SetColor(face, _quantity(face_color), XCAFDoc_ColorType.XCAFDoc_ColorSurf)
    _quiet()
    Interface_Static.SetCVal_s("write.step.unit", "MM")
    writer = STEPCAFControl_Writer()
    writer.SetNameMode(True)
    writer.SetColorMode(True)
    if not writer.Transfer(document, STEPControl_AsIs):
        raise ValueError("OpenCascade couldn't write it as STEP")
    path.parent.mkdir(parents=True, exist_ok=True)
    if writer.Write(str(path)) != IFSelect_ReturnStatus.IFSelect_RetDone:
        raise ValueError(f"Couldn't write {path.as_posix()}")


def _quantity(color: tuple[float, float, float, float]):
    from OCP.Quantity import Quantity_Color, Quantity_TypeOfColor

    # Appearances are sRGB, as a page's colors are
    return Quantity_Color(color[0], color[1], color[2], Quantity_TypeOfColor.Quantity_TOC_sRGB)


def _quiet() -> None:
    """Stops OpenCascade printing a line for each file it writes (its failures are still raised)."""
    from OCP.Message import Message, Message_Gravity

    printers = Message.DefaultMessenger_s().Printers()
    for index in range(1, printers.Length() + 1):
        printers.Value(index).SetTraceLevel(Message_Gravity.Message_Fail)
