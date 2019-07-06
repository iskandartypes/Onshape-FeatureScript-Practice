FeatureScript 1096;
import(path : "onshape/std/geometry.fs", version : "1096.0");

//define lego dimensions; not using ValueWithUnits bc it breaks vector calc, but all in mm
const brickBase is number = 8;
const height is number = 9.6;
const thick is number = 1.6;
const studDiam is number = 4.8;

annotation { "Feature Type Name" : "Lego Brick", "Filter Selector" : ["lego", "brick"] }
export const legoBrick = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {

        // Parameters
        annotation { "Name" : "Length" }
        isInteger(definition.length, POSITIVE_COUNT_BOUNDS);
        annotation { "Name" : "Width" }
        isInteger(definition.width, POSITIVE_COUNT_BOUNDS);
        annotation { "Name" : "Text at Top", "Default" : "lego", "MaxLength" : 4 }
        definition.text is string;

    }

    {
        //plane to project studs and top and top holes from
        var upPlane is Plane = plane(vector(0, 0, height) * millimeter, vector(0, 0, 1));
        //and text
        var textPlane is Plane = plane(vector(0, 0, height + thick) * millimeter, vector(0, 0, 1));

        //define sketches

        //outside walls
        var baseSketch is Sketch = newSketch(context, id + "skBase", {
                "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
            });
        //support posts
        var postSketch is Sketch = newSketch(context, id + "skPosts", {
                "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
            });
        //top of brick w/dimples
        var topBaseSketch is Sketch = newSketchOnPlane(context, id + "skTopBase", {
                "sketchPlane" : upPlane
            });
        //studs
        var studSketch is Sketch = newSketchOnPlane(context, id + "skStuds", {
                "sketchPlane" : upPlane
            });
        var textSketch is Sketch = newSketchOnPlane(context, id + "skText", {
                "sketchPlane" : textPlane
            });

        drawBases(baseSketch, topBaseSketch, definition.length, definition.width);
        drawStudsDimplesPostsText(studSketch, topBaseSketch, postSketch, textSketch, definition.length, definition.width, definition.text);

        //solve sketches

        skSolve(baseSketch);
        skSolve(postSketch);
        skSolve(topBaseSketch);
        skSolve(studSketch);
        skSolve(textSketch);

        //extrude and merges all sketches, set material to ABS plastic
        makeLego(context, id, baseSketch, postSketch, topBaseSketch, studSketch, textSketch, definition.length > 1 || definition.width > 1, definition.text != "");

        //delete sketches
        opDeleteBodies(context, id + "deleteBodies", { "entities" : qSketchFilter(qCreatedBy(id), SketchObject.YES) });

    });



//these functions exist not for reusability but for modularity

//drawBases draws rectangular sketches on the bases
//baseSketch is the sketch of the outside walls
//topBaseSketch is the sketch of the top flat of the lego
//len/wid are the length and width of the lego in studs

function drawBases(baseSketch is Sketch, topBaseSketch is Sketch, len is number, wid is number)
{
    skRectangle(baseSketch, "outerRec", {
                "firstCorner" : vector(0, 0) * millimeter,
                "secondCorner" : vector(brickBase * len, brickBase * wid) * millimeter
            });
    skRectangle(baseSketch, "innerRec", {
                "firstCorner" : vector(thick, thick) * millimeter,
                "secondCorner" : vector(brickBase * len - thick, brickBase * wid - thick) * millimeter
            });
    //draw the same rectangle on top
    skRectangle(topBaseSketch, "topRec", {
                "firstCorner" : vector(0, 0) * millimeter,
                "secondCorner" : vector(brickBase * len, brickBase * wid) * millimeter
            });
}

//loop through the dimensions;
//draw postSketch (posts), topBaseSketch, studSketch, and textSketch on top of studs
//currently a little hacky;
//length and width are doing essentially the same thing, but mirrored

function drawStudsDimplesPostsText(studSketch is Sketch, topBaseSketch is Sketch, postSketch is Sketch, textSketch is Sketch, len is number, wid is number, text is string)
{
    //length of text (stored so it doesn't have to be recalculated per op)
    var textLen is number = length(text);

    for (var i = 0; i < len; i += 1)
    {
        for (var j = 0; j < wid; j += 1)
        {
            //draws stud, dimple, text
            perStud(topBaseSketch, studSketch, textSketch, text, textLen, i, j);
            if (i == 0 && j == 0)
            {
                continue;
            }

            //the posts!
            perPost(postSketch, len, wid, i, j);

        }
    }
}

//draws stud, dimple, and text on relevant sketches per stud
//topBaseSketch is for top and dimples
//studSketch is for studs themselves
//textSketch is for text on studs
//i, j are coordinates (x, y) that correspond to i and j in above loop

function perStud(topBaseSketch is Sketch, studSketch is Sketch, textSketch is Sketch, text is string, textLen is number, i is number, j is number)
{
    //dimple and stud
    skCircle(topBaseSketch, "dimp" ~ i ~ "x" ~ j, {
                "center" : vector((brickBase / 2) + (brickBase * i), (brickBase / 2) + (brickBase * j)) * millimeter,
                "radius" : (thick / 2) * millimeter
            });
    skCircle(studSketch, "stud" ~ i ~ "x" ~ j, {
                "center" : vector((brickBase / 2) + (brickBase * i), (brickBase / 2) + (brickBase * j)) * millimeter,
                "radius" : (studDiam / 2) * millimeter
            });
    skText(textSketch, "texttop" ~ i ~ "x" ~ j, {
                "text" : text,
                "fontName" : "AllertaStencil-Regular.ttf",
                "firstCorner" : vector((brickBase / 2) + (brickBase * i) - thick, (brickBase / 2) + (brickBase * j) - thick / textLen) * millimeter,
                "secondCorner" : vector((brickBase / 2) + (brickBase * i) + thick, (brickBase / 2) + (brickBase * j) + thick / textLen) * millimeter
            });
}

//draws either small posts for (len == 1 || wid == 1) && (len!=wid), or big posts with holes
//postSketch is sketch for posts
//len, wid are dimensions of lego
//i, j are coordinates (x, y) that correspond to i and j in above loop

function perPost(postSketch is Sketch, len is number, wid is number, i is number, j is number)
{
    if (len == 1)
    {
        skCircle(postSketch, "smallPost" ~ j, {
                    "center" : vector(brickBase / 2, brickBase * j) * millimeter,
                    "radius" : thick * millimeter
                });
    }
    else if (wid == 1)
    {
        skCircle(postSketch, "smallPost" ~ i, {
                    "center" : vector(brickBase * i, brickBase / 2) * millimeter,
                    "radius" : thick * millimeter
                });
    }
    else if (i != 0 && j != 0)
    {
        skCircle(postSketch, "inner" ~ i ~ "x" ~ j, {
                    "center" : vector(brickBase * i, brickBase * j) * millimeter,
                    "radius" : (studDiam / 2) * millimeter
                });
        skCircle(postSketch, "outer" ~ i ~ "x" ~ j, {
                    "center" : vector(brickBase * i, brickBase * j) * millimeter,
                    "radius" : ((studDiam + thick) / 2) * millimeter
                });
    }
}

//extrudes all sketches to corresponding thicknesses, then merges
//takes context and id from parent
//sketches are solved sketches
//hasPost and hasText are booleans that check to see if those sketches should be extruded at all
//material of resultant union set to ABS

function makeLego(context is Context, id is Id, baseSketch is Sketch, postSketch is Sketch, topBaseSketch is Sketch, studSketch is Sketch, textSketch is Sketch, hasPost is boolean, hasText is boolean)
{
    opExtrude(context, id + "extrudeBase", {
                "entities" : qSketchRegion(id + "skBase", true),
                "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skBase") }).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : height * millimeter
            });
    opExtrude(context, id + "extrudeTopBase", {
                "entities" : qSketchRegion(id + "skTopBase", true),
                "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skTopBase") }).normal * -1,
                "endBound" : BoundingType.BLIND,
                "endDepth" : thick * millimeter
            });
    opExtrude(context, id + "extrudeStuds", {
                "entities" : qSketchRegion(id + "skStuds"),
                "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skStuds") }).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : thick * millimeter
            });
    //without these conditional, doesn't resolve; FS does not like extruding blank sketches
    if (hasPost)
    {
        opExtrude(context, id + "extrudePost", {
                    "entities" : qSketchRegion(id + "skPosts", true),
                    "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skPosts") }).normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : height * millimeter
                });
    }
    if (hasText)
    {
        opExtrude(context, id + "extrudeText", {
                    "entities" : qSketchRegion(id + "skText", true),
                    "direction" : evOwnerSketchPlane(context, { "entity" : qSketchRegion(id + "skText", true) }).normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : 0.2 * millimeter
                });
    }
    
    //UNIONIZE

        opBoolean(context, id + "booleanMergeAll", {
                    "tools" : qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                    "operationType" : BooleanOperationType.UNION
                });
                
    //PLASTICIZE
    
    setProperty(context, {
                    "entities" : qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                    "propertyType" : PropertyType.MATERIAL,
                    "value" : material("ABS", .001052 * gram / millimeter ^ 3)
                });

}

