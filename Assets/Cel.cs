using System;
using UnityEngine;

[Serializable]
public class Cel {

    public float MaxMass = 1.0f; //The normal, un-pressurized mass of a full water cell
    public float MaxCompress = 0.02f; //How much excess water a cell can store, compared to the cell above it
    public float MinMass = 0.0001f; //Ignore cells that are almost dry
    public float MinFlow = 0f;
    public float MaxSpeed = 5f;

    //Returns the amount of water that should be in the bottom cell.
    float get_stable_state_b(float total_mass) {
        if (total_mass <= 1) {
            return 1;
        } else if (total_mass < 2 * MaxMass + MaxCompress) {
            return (MaxMass * MaxMass + total_mass * MaxCompress) / (MaxMass + MaxCompress);
        } else {
            return (total_mass + MaxCompress) / 2;
        }
    }

    public void simulate_compression(Vector4[, , ] mass) {
        float Flow = 0;
        float remaining_mass;

        var map_width = mass.GetLength(0) - 1;
        var map_height = mass.GetLength(1) - 1;
        var map_depth = mass.GetLength(2) - 1;

        for (int x = 0; x < map_width; x++)
            for (int y = 0; y < map_height; y++)
                for (int z = 0; z < map_depth; z++)
                    mass[x, y, z].w = 1f - mass[x, y, z].w;

        var new_mass = (Vector4[, , ])mass.Clone();

        //Calculate and apply flow for each block
        for (int x = 1; x < map_width; x++)
            for (int y = 1; y < map_height; y++)
                for (int z = 1; z < map_depth; z++) {
                    //Skip inert ground blocks
                    // if (blocks[x][y] == GROUND)continue;
                    if (y == 0)continue;

                    //Custom push-only flow
                    Flow = 0;
                    remaining_mass = mass[x, y, z].w;
                    if (remaining_mass <= 0)continue;

                    //The block below this one
                    // if ((blocks[x, y - 1] != GROUND)) {
                    if (y > 0) {
                        Flow = get_stable_state_b(remaining_mass + mass[x, y - 1, z].w) - mass[x, y - 1, z].w;
                        if (Flow > MinFlow) {
                            Flow *= 0.5f; //leads to smoother flow
                        }
                        Flow = Mathf.Clamp(Flow, 0f, Mathf.Min(MaxSpeed, remaining_mass));

                        new_mass[x, y, z].w -= Flow;
                        new_mass[x, y - 1, z].w += Flow;
                        remaining_mass -= Flow;
                    }

                    if (remaining_mass <= 0f)continue;

                    //Left
                    // if (blocks[x - 1, y] != GROUND) {
                    if (x > 0 && x < map_width) {
                        //Equalize the amount of water in this block and it's neighbour
                        Flow = (mass[x, y, z].w - mass[x - 1, y, z].w) / 4;
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                        new_mass[x, y, z].w -= Flow;
                        new_mass[x - 1, y, z].w += Flow;
                        remaining_mass -= Flow;
                    }

                    if (remaining_mass <= 0)continue;

                    //Right
                    // if (blocks[x + 1, y] != GROUND) {
                    if (x > 0 && x < map_width) {
                        //Equalize the amount of water in this block and it's neighbour
                        Flow = (mass[x, y, z].w - mass[x + 1, y, z].w) / 4;
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                        new_mass[x, y, z].w -= Flow;
                        new_mass[x + 1, y, z].w += Flow;
                        remaining_mass -= Flow;
                    }

                    //Forward
                    // if (blocks[x, y, z + 1] != GROUND) {
                    if (z > 0 && z < map_depth) {
                        //Equalize the amount of water in this block and it's neighbour
                        Flow = (mass[x, y, z].w - mass[x, y, z + 1].w) / 4;
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                        new_mass[x, y, z].w -= Flow;
                        new_mass[x, y, z + 1].w += Flow;
                        remaining_mass -= Flow;
                    }

                    //Back
                    // if (blocks[x, y, z - 1] != GROUND) {
                    if (z > 0 && z < map_depth) {
                        //Equalize the amount of water in this block and it's neighbour
                        Flow = (mass[x, y, z].w - mass[x, y, z - 1].w) / 4;
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                        new_mass[x, y, z].w -= Flow;
                        new_mass[x, y, z - 1].w += Flow;
                        remaining_mass -= Flow;
                    }

                    if (remaining_mass <= 0)continue;

                    //Up. Only compressed water flows upwards.
                    // if (blocks[x, y + 1] != GROUND) {
                    Flow = remaining_mass - get_stable_state_b(remaining_mass + mass[x, y + 1, z].w);
                    if (Flow > MinFlow) { Flow *= 0.5f; }
                    Flow = Mathf.Clamp(Flow, 0, Mathf.Min(MaxSpeed, remaining_mass));

                    new_mass[x, y, z].w -= Flow;
                    new_mass[x, y + 1, z].w += Flow;
                    remaining_mass -= Flow;
                    // }

                }

        //Copy the new mass values to the mass array
        for (int x = 0; x < map_width; x++)
            for (int y = 0; y < map_height; y++)
                for (int z = 0; z < map_depth; z++)
                    mass[x, y, z].w = 1f - new_mass[x, y, z].w;

        // for (int x = 1; x <= map_width; x++) {
        //     for (int y = 1; y <= map_height; y++) {
        //         //Skip ground blocks
        //         if (blocks[x][y] == GROUND)continue;
        //         //Flag/unflag water blocks
        //         if (mass[x][y] > MinMass) {
        //             blocks[x][y] = WATER;
        //         } else {
        //             blocks[x][y] = AIR;
        //         }
        //     }
        // }

        //Remove any water that has left the map
        // for (int x = 0; x < map_width + 2; x++) {
        //     mass[x][0] = 0;
        //     mass[x][map_height + 1] = 0;
        // }
        // for (int y = 1; y < map_height + 1; y++) {
        //     mass[0][y] = 0;
        //     mass[map_width + 1][y] = 0;
        // }

    }

}