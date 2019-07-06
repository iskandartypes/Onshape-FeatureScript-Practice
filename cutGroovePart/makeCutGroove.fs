FeatureScript 1096;
import(path : "onshape/std/geometry.fs", version : "1096.0");

annotation { "Feature Type Name" : "Make 3-3" }
export const threeThree = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Length" }
        isLength(definition.len, { (millimeter) : [80.0, 96, 150] } as LengthBoundSpec);
        annotation { "Name" : "Width" }
        isLength(definition.wid, { (millimeter) : [45.0, 64, 128] } as LengthBoundSpec);
        annotation { "Name" : "Base Height" }
        isLength(definition.baseHt, { (millimeter) : [1.0, 16, 35] } as LengthBoundSpec);
        annotation { "Name" : "Back Width" }
        isLength(definition.bkWid, { (millimeter) : [1.0, 16, 35] } as LengthBoundSpec);
        annotation { "Name" : "Groove Structure Width" }
        isLength(definition.grWid, { (millimeter) : [32.0, 40, 60] } as LengthBoundSpec);
        annotation { "Name" : "Groove Length" }
        isLength(definition.grLen, { (millimeter) : [1.0, 42, 64] } as LengthBoundSpec);
        annotation { "Name" : "Groove Radius" }
        isLength(definition.grRad, { (millimeter) : [1.0, 12, 64] } as LengthBoundSpec);
        annotation { "Name" : "Groove Side Width" }
        isLength(definition.grSideWid, { (millimeter) : [1.0, 8, 15] } as LengthBoundSpec);
        annotation { "Name" : "Groove/Slope Height" }
        isLength(definition.grSlHt, { (millimeter) : [1.0, 28, 48] } as LengthBoundSpec);
        annotation { "Name" : "Slope Width" }
        isLength(definition.slWid, { (millimeter) : [1.0, 12, 64] } as LengthBoundSpec);
        annotation { "Name" : "Cut Length" }
        isLength(definition.cutLen, { (millimeter) : [1.0, 20, 32] } as LengthBoundSpec);
        annotation { "Name" : "Cut Width" }
        isLength(definition.cutWid, { (millimeter) : [1.0, 16, 35] } as LengthBoundSpec);
        annotation { "Name" : "Bottom Right Width" }
        isLength(definition.btrWid, { (millimeter) : [1.0, 16, 35] } as LengthBoundSpec);
        annotation { "Name" : "Back Curve Radius" }
        isLength(definition.bcurRad, { (millimeter) : [12.0, 32, 44] } as LengthBoundSpec);
    }
    {

        drawExtrudeBase(context, id, definition);
        drawExtrudeFront(context, id, definition);
        drawExtrudeRight(context, id, definition);

        //take intersection of all
        opBoolean(context, id + "booleanIntersectAll", {
                    "tools" : qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                    "operationType" : BooleanOperationType.INTERSECTION
                });

        //delete sketches
        opDeleteBodies(context, id + "deleteBodies", { "entities" : qSketchFilter(qCreatedBy(id), SketchObject.YES) });
    });

//We're going to draw some of these more or less manually.
//We're putting in lines along systems of equations;
//each vertex is calculated via a variable relative to the origin.
//due to the nature of the sketch tools, there aren't really better ways to deal with curves;
//it takes more processing power and effort to get sketch entities to subtract properly in fs than to figure out the equations.

//draws three base sketches, extrudes them to different heights, and then merges them
//context and id are from parent
//definition is of params passed in; they're converted to unitless numbers for easier vector calculation/typing

function drawExtrudeBase(context is Context, id is Id, definition is map)
{

    var baseSketch is Sketch = newSketch(context, id + "skBase", {
            "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });
    var backBaseSketch is Sketch = newSketch(context, id + "skBackBase", {
            "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });
    var grooveBaseSketch is Sketch = newSketch(context, id + "skGrooveBase", {
            "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });

    var len is number = definition.len / millimeter;
    var wid is number = definition.wid / millimeter;
    var baseHt is number = definition.baseHt / millimeter;
    var bkWid is number = definition.bkWid / millimeter;
    var grWid is number = definition.grWid / millimeter;
    var grLen is number = definition.grLen / millimeter;
    var grRad is number = definition.grRad / millimeter;
    var grSlHt is number = definition.grSlHt / millimeter;
    var slWid is number = definition.slWid / millimeter;
    var cutLen is number = definition.cutLen / millimeter;
    var cutWid is number = definition.cutWid / millimeter;
    var btrWid is number = definition.btrWid / millimeter;
    var bcurRad is number = definition.bcurRad / millimeter;


    //draw initial base sketch; it's shaped like a sideways 'h' and has a slot in it

    skLineSegment(baseSketch, "lineLeftUp", {
                "start" : vector(bkWid, 0) * millimeter,
                "end" : vector(bkWid, wid - grLen) * millimeter
            });
    skLineSegment(baseSketch, "lineLeftAcross", {
                "start" : vector(bkWid, wid - grLen) * millimeter,
                "end" : vector(bkWid + grWid, wid - grLen) * millimeter
            });
    skLineSegment(baseSketch, "lineMidUp", {
                "start" : vector(bkWid + grWid, wid - grLen) * millimeter,
                "end" : vector(bkWid + grWid, wid - slWid) * millimeter
            });
    skLineSegment(baseSketch, "lineRightAcross", {
                "start" : vector(bkWid + grWid, wid - slWid) * millimeter,
                "end" : vector(len, wid - slWid) * millimeter
            });
    //upper right vertex (len, wid - slWid)
    skLineSegment(baseSketch, "lineRightDown", {
                "start" : vector(len, wid - slWid) * millimeter,
                "end" : vector(len, btrWid + cutWid) * millimeter
            });
    //slot
    skLineSegment(baseSketch, "lineSlotIn", {
                "start" : vector(len, btrWid + cutWid) * millimeter,
                "end" : vector(len - cutLen, btrWid + cutWid) * millimeter
            });
    skArc(baseSketch, "arcSlot", {
                "start" : vector(len - cutLen, btrWid + cutWid) * millimeter,
                "mid" : vector(len - cutLen - (cutWid / 2), btrWid + (cutWid / 2)) * millimeter,
                "end" : vector(len - cutLen, btrWid) * millimeter
            });
    skLineSegment(baseSketch, "lineSlotOut", {
                "start" : vector(len - cutLen, btrWid) * millimeter,
                "end" : vector(len, btrWid) * millimeter
            });
    skLineSegment(baseSketch, "lineRightDown2", {
                "start" : vector(len, btrWid) * millimeter,
                "end" : vector(len, 0) * millimeter
            });
    //bottom left vertex (len, 0)
    skLineSegment(baseSketch, "lineBottom", {
                "start" : vector(len, 0) * millimeter,
                "end" : vector(bkWid, 0) * millimeter
            });

    //draw backBaseSketch

    skRectangle(backBaseSketch, "rectangleBack", {
                "firstCorner" : vector(0, 0) * millimeter,
                "secondCorner" : vector(bkWid, wid) * millimeter
            });

    //draw grooveBaseSketch

    skRectangle(grooveBaseSketch, "rectangleGroove", {
                "firstCorner" : vector(bkWid, wid - grLen) * millimeter,
                "secondCorner" : vector(bkWid + grWid, wid) * millimeter
            });
    skRectangle(grooveBaseSketch, "rectangleSlope", {
                "firstCorner" : vector(bkWid + grWid, wid - slWid) * millimeter,
                "secondCorner" : vector(len, wid) * millimeter
            });

    skSolve(baseSketch);
    skSolve(backBaseSketch);
    skSolve(grooveBaseSketch);

    //extrude/merge base
    opExtrude(context, id + "extrudeBase", {
                "entities" : qSketchRegion(id + "skBase"),
                "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skBase") }).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : baseHt * millimeter
            });
    opExtrude(context, id + "extrudeBackBase", {
                "entities" : qSketchRegion(id + "skBackBase"),
                "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skBackBase") }).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : (baseHt + grSlHt - grRad + bcurRad) * millimeter
            });
    opExtrude(context, id + "extrudeGrooveBase", {
                "entities" : qSketchRegion(id + "skGrooveBase"),
                "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skGrooveBase") }).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : (baseHt + grSlHt) * millimeter
            });
    opBoolean(context, id + "booleanMergeBase", {
                "tools" : qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                "operationType" : BooleanOperationType.UNION
            });
}

//draws and then extrudes front sketch to width
//context and id are from parent
//definition is of params passed in; they're converted to unitless numbers for easier vector calculation

function drawExtrudeFront(context is Context, id is Id, definition is map)
{
    var frontSketch is Sketch = newSketch(context, id + "skFront", {
            "sketchPlane" : qCreatedBy(makeId("Front"), EntityType.FACE)
        });

    var len is number = definition.len / millimeter;
    var wid is number = definition.wid / millimeter;
    var baseHt is number = definition.baseHt / millimeter;
    var bkWid is number = definition.bkWid / millimeter;
    var grWid is number = definition.grWid / millimeter;
    var grRad is number = definition.grRad / millimeter;
    var grSideWid is number = definition.grSideWid / millimeter;
    var grSlHt is number = definition.grSlHt / millimeter;
    var bcurRad is number = definition.bcurRad / millimeter;

    skRectangle(frontSketch, "rectangleBack", {
                "firstCorner" : vector(0, baseHt) * millimeter,
                "secondCorner" : vector(bkWid, baseHt + grSlHt - grRad + bcurRad) * millimeter
            });
    skRectangle(frontSketch, "rectangleBottom", {
                "firstCorner" : vector(0, 0) * millimeter,
                "secondCorner" : vector(len, baseHt) * millimeter
            });
    skLineSegment(frontSketch, "lineGrooveLeft", {
                "start" : vector(bkWid, baseHt + grSlHt) * millimeter,
                "end" : vector(bkWid + (grWid - (grRad * 2) - grSideWid), baseHt + grSlHt) * millimeter
            });
    skArc(frontSketch, "arcGroove", {
                "start" : vector(bkWid + grWid - (grRad * 2) - grSideWid, baseHt + grSlHt) * millimeter,
                "mid" : vector(bkWid + grWid - grRad - grSideWid, baseHt + grSlHt - grRad) * millimeter,
                "end" : vector(bkWid + grWid - grSideWid, baseHt + grSlHt) * millimeter
            });
    skLineSegment(frontSketch, "lineGrooveRight", {
                "start" : vector(bkWid + grWid - grSideWid, baseHt + grSlHt) * millimeter,
                "end" : vector(bkWid + grWid, baseHt + grSlHt) * millimeter
            });
    skLineSegment(frontSketch, "lineSlope", {
                "start" : vector(bkWid + grWid, baseHt + grSlHt) * millimeter,
                "end" : vector(len, baseHt) * millimeter
            });

    skSolve(frontSketch);

    //extrude front/right
    opExtrude(context, id + "extrudeFront", {
                "entities" : qSketchRegion(id + "skFront"),
                "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skFront") }).normal * -1,
                "endBound" : BoundingType.BLIND,
                "endDepth" : wid * millimeter
            });

}

//draws and extrudes right sketch to length
//context and id are from parent
//definition is of params passed in; they're converted to unitless numbers for easier vector calculation

function drawExtrudeRight(context is Context, id is Id, definition is map)
{
    var rightSketch is Sketch = newSketch(context, id + "skRight", {
            "sketchPlane" : qCreatedBy(makeId("Right"), EntityType.FACE)
        });

    var len is number = definition.len / millimeter;
    var wid is number = definition.wid / millimeter;
    var baseHt is number = definition.baseHt / millimeter;
    var grRad is number = definition.grRad / millimeter;
    var grSlHt is number = definition.grSlHt / millimeter;
    var bcurRad is number = definition.bcurRad / millimeter;

    //midpoint of a quarter-circle arc
    var backTrig is number = sqrt((bcurRad ^ 2) / 2);

    skRectangle(rightSketch, "rectangleBottom", {
                "firstCorner" : vector(0, 0) * millimeter,
                "secondCorner" : vector(wid, baseHt + grSlHt - grRad) * millimeter
            });
    skRectangle(rightSketch, "rectangleTop", {
                "firstCorner" : vector(wid - bcurRad, baseHt + grSlHt - grRad) * millimeter,
                "secondCorner" : vector(wid, baseHt + grSlHt - grRad + bcurRad) * millimeter
            });
    skArc(rightSketch, "arcBack", {
                "start" : vector(0, baseHt + grSlHt - grRad) * millimeter,
                //I think this scales acceptably; changes height but keeps circle center
                "mid" : vector(bcurRad - backTrig, baseHt + grSlHt - grRad + backTrig) * millimeter,
                "end" : vector(wid - bcurRad, baseHt + grSlHt - grRad + bcurRad) * millimeter
            });

    skSolve(rightSketch);

    opExtrude(context, id + "extrudeRight", {
                "entities" : qSketchRegion(id + "skRight"),
                "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skRight") }).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : len * millimeter
            });
}

