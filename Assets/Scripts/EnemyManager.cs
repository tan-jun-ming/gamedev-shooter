﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{

    public GameObject[] enemies;
    public GameObject enemy_ufo;

    private int formation_width = 11;
    private int formation_height = 5;

    private float left_boundary = -92f;
    private float right_boundary = 76f;
    private float top_boundary = 60f;
    private float bot_boundary = 100f;

    private float enemy_width = 12f;
    private float enemy_height = 8f;
    private float enemy_pad_x = 4f;
    private float enemy_pad_y = 8f;

    private int direction = 1;
    private int next_dir = -1;

    private float ufo_start_y = 84f;
    private int ufo_dir = 1;
    private bool ufo_active = false;
    private GameObject active_ufo;

    private List<List<Enemy>> formation;

    private int x_formation_bound_beg;
    private int x_formation_bound_end;

    private int y_formation_bound_beg;
    private int y_formation_bound_end;

    private int max_freeze = 70;
    private int freeze = 0;

    private int enemy_step_counter = 0;
    private bool turn = false;

    // Start is called before the first frame update
    void Start()
    {
        x_formation_bound_beg = 0;
        x_formation_bound_end = formation_width - 1;

        y_formation_bound_beg = 0;
        y_formation_bound_end = formation_height - 1;

        formation = new List<List<Enemy>>();

        for (int i = 0; i < formation_height; i++)
        {
            formation.Add(new List<Enemy>());
            for (int u = 0; u < formation_width; u++)
            {
                formation[i].Add(initialize_enemy(i, u, calculate_enemy_type(i)));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (ufo_active)
        {
            step_ufo();
        }

        if (Input.GetMouseButtonDown(0))
        {

            float distance = 25f;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, distance);

            if (hit)
            {
                if (hit.transform.CompareTag("Enemy"))
                {
                    ((Enemy)hit.transform.gameObject.GetComponent(typeof(Enemy))).kill();
                }
            }

        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            spawn_ufo();
        }

        if (freeze > 0)
        {
            freeze--;
            return;
        }

        step_enemies();

    }

    void step_enemies()
    {
        int counter = -1;
        bool stepped = false;

        for (int i = y_formation_bound_end; i >= y_formation_bound_beg; i--)
        {
            for (int u = x_formation_bound_beg; u <= x_formation_bound_end; u++)
            {
                counter++;

                if (counter >= enemy_step_counter)
                {
                    enemy_step_counter = counter + 1;

                    Enemy enemy = formation[i][u];
                    if (!enemy.dead)
                    {
                        enemy.step(direction, turn);
                        stepped = true;
                        break;
                    }
                }
            }
            if (stepped)
            {
                break;
            }
        }

        if (!stepped)
        {
            enemy_step_counter = 0;

            if (turn)
            {
                turn = false;
            }

            for (int i = y_formation_bound_beg; i <= y_formation_bound_end; i++)
            {
                int column_to_check;
                if (direction < 0)
                {
                    column_to_check = x_formation_bound_beg;
                    Enemy en = formation[i][column_to_check];
                    if (!en.dead && en.get_pos().x <= left_boundary)
                    {
                        change_direction();
                        break;
                    }
                } else if (direction > 0)
                {
                    column_to_check = x_formation_bound_end;
                    Enemy en = formation[i][column_to_check];
                    if (!en.dead && formation[i][column_to_check].get_pos().x >= right_boundary)
                    {
                        change_direction();
                        break;
                    }
                }
            }
        }
    }

    void change_direction()
    {
        turn = true;
        direction *= -1;
    }

    void spawn_ufo()
    {
        if (ufo_active)
        {
            return;
        }

        ufo_active = true;

        ufo_dir = Random.Range(0, 2);
        Vector3 ufo_start_pos = Vector3.up * ufo_start_y;
        ufo_start_pos.x = left_boundary;

        if (ufo_dir == 0)
        {
            ufo_dir = -1;
            ufo_start_pos.x = right_boundary;
        }

        active_ufo = GameObject.Instantiate(enemy_ufo, ufo_start_pos, Quaternion.Euler(0, 0, 0));
        Enemy ufo = (Enemy)active_ufo.GetComponent(typeof(Enemy));
        ufo.formation_x = -1;
        ufo.formation_y = -1;
        ufo.manager = this;
        ufo.max_death_counter = max_freeze;
        ufo.points_worth = Random.Range(1, 4) * 50;

    }

    void step_ufo()
    {
        if (!ufo_active)
        {
            return;
        }

        if ( (ufo_dir == -1 && active_ufo.transform.position.x <= left_boundary) ||
            (ufo_dir == 1 && active_ufo.transform.position.x >= right_boundary)
            )
        {
            GameObject.Destroy(active_ufo);
            ufo_active = false;
            return;
        }

        ((Enemy)active_ufo.GetComponent(typeof(Enemy))).step(ufo_dir);

    }
    public void report_death(int x, int y, int points)
    {
        print(points + " Points!");

        if (x < 0)
        {
            ufo_active = false;
            return;
        }

        freeze = max_freeze;

        if (x == x_formation_bound_beg)
        {
            if (check_col_dead(x_formation_bound_beg))
            {
                x_formation_bound_beg++;
            }
        } else if (x == x_formation_bound_end)
        {
            if (check_col_dead(x_formation_bound_end))
            {
                x_formation_bound_end--;
            }
        }

        if (y == y_formation_bound_beg)
        {
            if (check_row_dead(y_formation_bound_beg))
            {
                y_formation_bound_beg++;
            }
        } else if (y == y_formation_bound_end)
        {
            if (check_row_dead(y_formation_bound_end))
            {
                y_formation_bound_end--;
            }
        }

        if (x_formation_bound_beg > x_formation_bound_end)
        {
            // do something when all enemies defeated
        }
    }

    bool check_row_dead(int r)
    {
        for (int i=x_formation_bound_beg; i<=x_formation_bound_end; i++)
        {
            if (!formation[r][i].dead)
            {
                return false;
            }
        }

        return true;
    }

    bool check_col_dead(int c)
    {
        for (int i = y_formation_bound_beg; i <= y_formation_bound_end; i++)
        {
            if (!formation[i][c].dead)
            {
                return false;
            }
        }

        return true;
    }
    int calculate_enemy_type(int row)
    {
        int[] choices = { 0, 1, 1, 2, 2, 0, 0, 0, 0, 0, 0 };
        return choices[row]; // Maybe do proper calculations in the future
    }

    Enemy initialize_enemy(int row, int column, int type)
    {
        Vector3 new_coordinates = Vector3.zero;

        new_coordinates.x = left_boundary + column * (enemy_width + enemy_pad_x);
        new_coordinates.y = top_boundary - row * (enemy_height + enemy_pad_y);

        GameObject new_enemy = GameObject.Instantiate(enemies[type], new_coordinates, Quaternion.Euler(0, 0, 0));

        Enemy ret = (Enemy)new_enemy.GetComponent(typeof(Enemy));

        ret.formation_x = column;
        ret.formation_y = row;

        ret.max_death_counter = max_freeze;
        ret.y_step = enemy_height;

        ret.manager = this;

        return ret;
    }
}