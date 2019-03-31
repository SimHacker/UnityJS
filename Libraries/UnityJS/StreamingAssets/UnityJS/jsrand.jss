/*
  jsrand v1.2
  https://github.com/DomenicoDeFelice/jsrand
  @license none/public domain, just leave this comment.
*/

!function (global) {

if (!global.dfd) {
    global.dfd = {};
}

dfd.Srand = Srand = (function (oldValue) {
    var constructor = function (seed) {
        if (seed !== undefined) {
            this.seed(seed);
        } else {
            this.randomize();
        }
    };

    constructor._oldSrand = oldValue;

    return constructor;
})(global.Srand);

Srand.prototype = {};

// Sets or gets (if no argument is given) the seed
// for the pseudo-random number generator.
// The seed can be any float or integer number.
Srand.seed = Srand.prototype.seed = function (seed) {
    if (seed === undefined) {
        return this._seed;
    }

    // Uses only one seed (mw), mz is fixed.
    // Must not be zero, nor 0x9068ffff.
    this._mz = 123456789;

    return this._mw = this._seed = seed;
};

// Sets and returns a random seed.
Srand.randomize = Srand.prototype.randomize = function () {
    var seed = this.randomIntegerIn(1, 0xffffffff, Math.random());
    this.seed(seed);

    return seed;
};

// Returns a pseudo-random number between 0 inclusive and 1 exclusive.
// Algorithm used is MWC (multiply-with-carry) by George Marsaglia.
// Implementation based on:
// - http://en.wikipedia.org/wiki/Random_number_generation#Computational_methods
// - http://stackoverflow.com/questions/521295/javascript-random-seeds#19301306
Srand.random = Srand.prototype.random = function () {
    if (this._seed === undefined) {
        this.randomize();
    }

    var mz = this._mz;
    var mw = this._mw;

    // The 16 least significant bits are multiplied by a constant
    // and then added to the 16 most significant bits. 32 bits result.
    mz = ((mz & 0xffff) * 36969 + (mz >> 16)) & 0xffffffff;
    mw = ((mw & 0xffff) * 18000 + (mw >> 16)) & 0xffffffff;

    this._mz = mz; 
    this._mw = mw;

    var x = (((mz << 16) + mw) & 0xffffffff) / 0x100000000;
    return 0.5 + x;
};


// Utility function that returns a random float number
// between a (inclusive) and b (exclusive).
// If `x` is specified, it is used as the random number
// (between 0 inclusive and 1 exclusive, e.g., Math.random()
// could be passed as argument).
// If `x` is not specified, object/Srand random() is used.
Srand.randomIn = Srand.prototype.randomIn = function (a, b, x) {
    if (x === undefined) {
        x = this.random();
    }

    return a + x * (b - a);
};

// Utility function that returns a random integer between
// min and max inclusive.
// If `x` is specified, it is used as the random number
// (between 0 inclusive and 1 exclusive, e.g., Math.random()
// could be passed as argument).
// If `x` is not specified, object/Srand random() is used.
Srand.randomIntegerIn = Srand.prototype.randomIntegerIn = function (min, max, x) {
    if (x === undefined) {
        x = this.random();
    }

    return min + Math.floor(x * (max - min + 1));
};

// Utility function that returns a random element from the array
// passed as argument.
// If `x` is specified, it is used as the random number (between 0
// inclusive and 1 exclusive, e.g., Math.random() could be passed as
// argument).
// If `x` is not specified, object/Srand random() is used.
Srand.choice = Srand.prototype.choice = function (arr, x) {
    if (arr.length === 0) {
        return undefined;
    }

    var randomIndex = this.randomIntegerIn(0, arr.length-1, x);

    return arr[randomIndex];
};

// In the uncommon case the variable `Srand` is already used,
// this function restores its initial value and returns the
// Srand object (dfd.Srand can be used as well).
Srand.noConflict = function () {
    Srand = dfd.Srand._oldSrand;
    return dfd.Srand;
};

}(window);
