FeatureScript 1096;
import(path : "onshape/std/geometry.fs", version : "1096.0");

annotation { "Feature Type Name" : "Make 2-4" }
export const twoFour = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Side" }
        isLength(definition.side, { (millimeter) : [80.0, 100, 1e5] } as LengthBoundSpec);
        annotation { "Name" : "Corner Radius" }
        isLength(definition.corner, { (millimeter) : [0.0, 10, 1e5] } as LengthBoundSpec);
        annotation { "Name" : "Spacing" }
        isLength(definition.space, { (millimeter) : [-1e5, 10, 1e5] } as LengthBoundSpec);
    }
    {
        // Define the function's action
        var sketch is Sketch = newSketch(context, id + "skMain", {
                "sketchPlane" : qCreatedBy(makeId("Front"), EntityType.FACE)
            });

        var side is number = definition.side / millimeter;
        var corner is number = definition.corner / millimeter;
        var space is number = definition.space / millimeter;

        for (var i = 0; i < 4; i += 1)
        {
            makeSquare(id + ("sq" ~ i) as Id, sketch, 0 + (space * i), corner, side - (space * (i * 2)) - corner * 2);
        }

        skSolve(sketch);
    });


//making the square for the sketch
//parameters:
//id is id of parent process
//sk is sketch square is made on
//rad is radius of corner
//side is parallel distance between sides of square
//--
//it draws a square with radius corners
function makeSquare(id is Id, sk is Sketch, start is number, rad is number, side is number)
{
    var strad is number = start + rad;
    var radside is number = rad + side;
    var arc is number = sqrt((rad ^ 2) / 2);

    //left line
    skLineSegment(sk, "line" ~ id[1] ~ .1, {
                "start" : vector(start, strad) * millimeter,
                "end" : vector(start, strad + side) * millimeter
            });
    //top line
    skLineSegment(sk, "line" ~ id[1] ~ .3, {
                "start" : vector(strad + radside, strad) * millimeter,
                "end" : vector(strad + radside, strad + side) * millimeter
            });
    //right line
    skLineSegment(sk, "line" ~ id[1] ~ .4, {
                "start" : vector(strad, strad + radside) * millimeter,
                "end" : vector(strad + side, strad + radside) * millimeter
            });
    //bottom line
    skLineSegment(sk, "line" ~ id[1] ~ .2, {
                "start" : vector(strad, start) * millimeter,
                "end" : vector(strad + side, start) * millimeter
            });

    //bottom left corner
    skArc(sk, "arc" ~ id[1] ~ .1, {
                "start" : vector(strad, start) * millimeter,
                "mid" : vector(strad - arc, strad - arc) * millimeter,
                "end" : vector(start, strad) * millimeter
            });
    //top left corner
    skArc(sk, "arc" ~ id[1] ~ .2, {
                "start" : vector(start, strad + side) * millimeter,
                "mid" : vector(strad - arc, strad + side + arc) * millimeter,
                "end" : vector(strad, strad + radside) * millimeter
            });
    //top right corner
    skArc(sk, "arc" ~ id[1] ~ .3, {
                "start" : vector(strad + side, strad + radside) * millimeter,
                "mid" : vector(strad + side + arc, strad + side + arc) * millimeter,
                "end" : vector(strad + radside, strad + side) * millimeter
            });
    //bottom right corner
    skArc(sk, "arc" ~ id[1] ~ .4, {
                "start" : vector(strad + radside, strad) * millimeter,
                "mid" : vector(strad + side + arc, strad - arc) * millimeter,
                "end" : vector(strad + side, start) * millimeter
            });
}

