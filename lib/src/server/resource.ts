import { Knex } from "knex";
import { Database, ResourceManagerConstructor } from "./database.js";
import { LogLevel } from "../shared/service.js";

export interface ResourceHolder {
  id: number;
}

export interface Resource<
  R extends Resource<R, M> = never,
  M extends ResourceManager<R, M> = never
> {
  dataId: number;
  id: number;
  createTime: number;

  previousDataId: number | null;
  nextDataId: number | null;
}

export interface ResourceRecordStats {
  id: number;
  createTime: number;
  updateTime: number;
}

export type ResourceManagerInitCallback = (
  onInit: (version?: number) => Promise<void>
) => void;

export abstract class ResourceManager<
  R extends Resource<R, M>,
  M extends ResourceManager<R, M>
> {
  constructor(
    db: Database,
    init: ResourceManagerInitCallback,
    name: string,
    version: number
  ) {
    this.#db = db;
    this.#name = name;

    if (version <= 0) {
      throw new Error("Invalid version");
    }
    this.#version = version;
    this.#transact = db.transact.bind(db);

    init((version) => this.#init(version));
  }

  readonly #db: Database;
  readonly #name: string;
  readonly #version: number;
  readonly #transact: Database["transact"];

  get transact() {
    return this.#transact;
  }

  get #RECORD_TABLE() {
    return `${this.#name}_Record`;
  }

  get #DATA_TABLE() {
    return `${this.#name}_Data`;
  }

  get db() {
    return this.#db.db;
  }

  get transacting() {
    return this.#db.transacting;
  }

  get name() {
    return this.#name;
  }

  get dataTableName() {
    return this.#DATA_TABLE;
  }

  get recordTableName() {
    return this.#RECORD_TABLE;
  }

  get version() {
    return this.#version;
  }

  get searchableColumns(): string[] {
    return [];
  }

  async #init(version: number = 0): Promise<void> {
    if (version === 0) {
      await this.logSql(
        'debug',
        this.db.schema.createTable(this.#RECORD_TABLE, (table) => {
          table.increments("id");
          table.boolean("deleted");
        })
      );

      await this.logSql(
        'debug',
        this.db.schema.createTable(this.#DATA_TABLE, (table) => {
          table.increments("dataId");
          table.integer("id").notNullable();
          table.integer("createTime").notNullable();

          table.integer("previousDataId").nullable();
          table.integer("nextDataId").nullable();
        })
      );
    }

    await this.logSql(
      'debug',
      this.db.schema.alterTable(this.#DATA_TABLE, (table) =>
        this.upgrade(table, version)
      )
    );
  }

  public getManager<R extends Resource<R, M>, M extends ResourceManager<R, M>>(
    init: ResourceManagerConstructor<R, M>
  ) {
    return this.#db.getManager<R, M>(init);
  }

  public getManagers<R extends ResourceManagerConstructor<any, any>[]>(
    ...constructors: R
  ) {
    return this.#db.getManagers(...constructors);
  }

  public insertMany(data: Omit<R, keyof Resource>[]): Promise<R[]> {
    return Promise.all(data.map((entry) => this.insert(entry)));
  }

  public async getStats(id: number): Promise<ResourceRecordStats | null> {
    const earliest = (await this.db
      .table<R, R[]>(this.dataTableName)
      .select("*")
      .where("id", "=", id)
      .where("previousDataId", "is", null)
      .first()) as R;

    if (earliest == null) {
      return null;
    }

    const latest = (await this.db
      .table<R, R[]>(this.dataTableName)
      .select("*")
      .where("id", "=", id)
      .where("nextDataId", "=", null)
      .first()) as R;

    return {
      createTime: earliest.createTime,
      updateTime: latest?.createTime ?? earliest.createTime,
      id,
    };
  }

  public async insert(data: Omit<R, keyof Resource>): Promise<R> {
    const resourceRecord = (
      await this.logSql(
        'debug',
        this.db
          .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
          .insert({ deleted: false })
          .returning("*")
      )
    )[0];

    const dataResult = await this.logSql(
      'debug',
      this.db
        .table<R, R[]>(this.#DATA_TABLE)
        .insert({
          id: resourceRecord.id,
          createTime: Date.now(),
          previousDataId: null,
          nextDataId: null,
          ...data,
        } as never)
        .returning("*")
    );

    return dataResult[0] as R;
  }

  public async getById(
    id: number,
    { includeDeleted = false, dataId }: GetByIdOptions<R, M> = {}
  ): Promise<R | null> {
    const resourceRecord = await this.logSql(
      'debug',
      this.db
        .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
        .where("id", "=", id)
        .first()
    );

    if (resourceRecord == null) {
      return null;
    } else if (!includeDeleted && resourceRecord.deleted) {
      return null;
    }

    if (dataId == null) {
      return (await this.logSql(
        'debug',
        this.db
          .table<R, R[]>(this.#DATA_TABLE)
          .where("id", "=", id)
          .where("nextDataId", "is", null)
          .first()
      ))! as R;
    }

    return (await this.logSql(
      'debug',
      this.db
        .table<R, R[]>(this.#DATA_TABLE)
        .where("id", "=", id)
        .where("dataId", "=", dataId)
        .first()
    ))! as R;
  }

  public async update(
    data: R,
    newData: Partial<Omit<R, keyof Resource>>,
    options?: UpdateOptions<R, M>
  ): Promise<R> {
    const resourceRecord = await this.logSql(
      'debug',
      this.db
        .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
        .where("id", "=", data.id)
        .first()
    );

    if (resourceRecord == null) {
      throw new Error("Resource not found");
    }

    if (resourceRecord.deleted === true) {
      throw new Error("Resource is deleted");
    }

    const base = await this.logSql(
      'debug',
      this.db
        .table<R, R[]>(this.#DATA_TABLE)
        .where(
          "dataId",
          "=",
          options?.baseDataId != null ? options.baseDataId : data.dataId
        )
        .first()
    );

    if (base == null) {
      throw new Error("Base version not found");
    }

    const insertRow: R = {
      ...base,
      ...newData,
      id: resourceRecord.id,
      createTime: Date.now(),
      previousDataId: base.dataId,
      nextDataId: null,
    } as never;

    delete (insertRow as Partial<R>).dataId;

    const newDataResult: R = (
      await this.logSql(
        'debug',
        this.db
          .table<R, R[]>(this.#DATA_TABLE)
          .insert(
            Object.assign(insertRow, {
              dataId: null,
            }) as never
          )
          .returning("*")
      )
    )[0] as never;

    await this.logSql(
      'debug',
      this.db
        .table<R, R[]>(this.#DATA_TABLE)
        .where("dataId", "=", data.dataId)
        .update({
          nextDataId: newDataResult.dataId,
        } as never)
    );

    data.nextDataId = newDataResult.dataId;

    return newDataResult;
  }

  public async delete(data: R): Promise<void> {
    await this.logSql(
      'debug',
      this.db
        .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
        .where("id", "=", data.id)
        .update({ deleted: true })
    );
  }

  public async updateWhere(
    set: Partial<Omit<R, keyof Resource>>,
    where: (WhereClause<R, M> | null)[]
  ): Promise<void> {
    const ids: number[] = [];

    for await (const resource of this.readStream({ where })) {
      ids.push(resource.id);
    }

    for (const id of ids) {
      const data = await this.getById(id);

      if (data == null) {
        continue;
      }

      await this.update(data, set);
    }
  }

  public async deleteWhere(where: (WhereClause<R, M> | null)[]): Promise<void> {
    let query = this.db.table<R, R[]>(this.#DATA_TABLE).select(["id as id"]);
    query = where.reduce((query, where) => {
      if (where == null) {
        return query;
      }

      const [column, op, value] = where;

      return query.where(column as never, op, value as never);
    }, query);

    await this.logSql(
      'debug',
      this.db
        .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
        .update({ deleted: true })
        .whereIn("id", query)
    );
  }

  public async restoreWhere(
    where: (WhereClause<R, M> | null)[]
  ): Promise<void> {
    let query = this.db.table<R, R[]>(this.#DATA_TABLE).select(["id as id"]);
    query = where.reduce((query, where) => {
      if (where == null) {
        return query;
      }

      const [column, op, value] = where;

      return query.where(column as never, op, value as never);
    }, query);

    await this.logSql(
      'debug',
      this.db
        .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
        .update({ deleted: false })
        .whereIn("id", query)
    );
  }

  public async purgeWhere(where: (WhereClause<R, M> | null)[]): Promise<void> {
    const ids: number[] = [];

    for await (const resource of this.readStream({ where })) {
      ids.push(resource.id);
    }

    for (const id of ids) {
      const data = await this.getById(id);
      if (data == null) {
        continue;
      }

      await this.purge(data);
    }
  }

  public async restore(data: R): Promise<void> {
    await this.logSql(
      'debug',
      this.db
        .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
        .where("id", "=", data.id)
        .update({ deleted: false })
    );
  }

  public async purge(data: R): Promise<void> {
    await this.logSql(
      'debug',
      this.db.table<R, R[]>(this.#DATA_TABLE).where("id", "=", data.id).delete()
    );

    await this.logSql(
      'debug',
      this.db
        .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
        .where("id", "=", data.id)
        .delete()
    );
  }

  public async count(where?: (WhereClause<R, M> | null)[]): Promise<number> {
    let query = this.db
      .from(
        this.db
          .table<R>(this.#DATA_TABLE)
          .select("*")
          .innerJoin(
            this.#RECORD_TABLE,
            this.#RECORD_TABLE + ".id",
            "=",
            this.#DATA_TABLE + ".id"
          )
      )
      .where("nextDataId", "is", null)
      .where("deleted", "=", false)
      .count("* as count")
      .first();

    if (where != null) {
      query = where.reduce((query, where) => {
        if (where == null) {
          return query;
        }

        const [key, op, value] = where;
        return query.where(key as never, op, value as never);
      }, query);
    }

    return (await this.logSql('debug', query))["count"] as number;
  }

  public async read(options?: QueryOptions<R, M>): Promise<R[]> {
    const result: R[] = [];

    for await (const record of this.readStream({
      includeDeleted: false,
      ...options,
    })) {
      result.push(record);
    }

    return result;
  }

  public async first(
    options?: Omit<QueryOptions<R, M>, "limit">
  ): Promise<R | null> {
    for await (const resource of this.readStream({ ...options, limit: 1 })) {
      return resource;
    }

    return null;
  }

  public async *readStream(options?: QueryOptions<R, M>): AsyncGenerator<R> {
    let offset = options?.offset ?? 0;
    let yielded = 0;

    while (yielded < (options?.limit ?? Infinity)) {
      let query = this.db.table<R, R[]>(this.#DATA_TABLE).select("*");

      query.andWhere((query) => {
        query.where("nextDataId", "is", null);

        if (options?.search != null) {
          query.andWhere((query) => {
            for (const searchableColumn of this.searchableColumns) {
              query.orWhere(searchableColumn, "like", `%${options.search}%`);
            }
          });
        }
      });

      if (options?.where != null) {
        query.andWhere((query) => {
          options.where!.reduce((query, where) => {
            if (where == null) {
              return query;
            }

            const [key, op, value] = where;
            return query.where(key as never, op, value as never);
          }, query);
          return query;
        });
      }

      if (options?.orderBy != null) {
        query = options.orderBy.reduce(
          (query, entry) =>
            entry != null
              ? query.orderBy(entry[0], entry[1] ?? false ? "desc" : "asc")
              : query,
          query
        );
      }

      query = query.offset(offset);
      query = query.limit(
        Math.min(
          options?.limit != null ? options.limit - yielded : Infinity,
          100
        )
      );

      const resources = await this.logSql('debug', query);

      if (resources.length === 0) {
        break;
      }

      for (const resource of resources) {
        if (options?.includeDeleted ?? false) {
          yield resource as R;
          yielded++;
          continue;
        }

        const resourceRecord = await this.logSql(
          'debug',
          this.db
            .table<ResourceRecord, ResourceRecord[]>(this.#RECORD_TABLE)
            .select("*")
            .where("id", "=", (resource as R).id)
            .first()
        );

        if (resourceRecord != null && !resourceRecord.deleted) {
          yield resource as R;
          yielded++;
        }
        offset++;
      }
    }
  }

  protected abstract upgrade(
    table: Knex.AlterTableBuilder,
    version: number
  ): void;

  public logSql<T extends Knex.QueryBuilder | Knex.SchemaBuilder | Knex.Raw>(
    level: LogLevel,
    query: T
  ): T {
    return this.#db.logSql(level, query);
  }
}

export interface ResourceRecord {
  id: number;
  deleted: boolean;
}

export interface UpdateOptions<
  R extends Resource<R, M>,
  M extends ResourceManager<R, M>
> {
  baseDataId?: number;
}

export interface DeleteOptions<
  R extends Resource<R, M>,
  M extends ResourceManager<R, M>
> {
  where?: WhereClause<R, M>[];
}

export interface GetByIdOptions<
  R extends Resource<R, M>,
  M extends ResourceManager<R, M>
> {
  includeDeleted?: boolean;
  dataId?: number;
}

export interface SearchOptions<
  R extends Resource<R, M>,
  M extends ResourceManager<R, M>
> {
  string: string;

  searchColumns: keyof R[];
}

export type QueryOptions<
  R extends Resource<R, M>,
  M extends ResourceManager<R, M>
> = {
  where?: (WhereClause<R, M> | null)[];
  search?: string;

  orderBy?: (OrderByClause<R, M> | null)[];

  offset?: number;
  limit?: number;

  includeDeleted?: boolean;
} & (
  | {
      where: (WhereClause<R, M> | null)[];
      search: null;
    }
  | {
      where: null;
      search: string;
    }
  | object
);

export type WhereClause<
  R extends Resource<R, M>,
  M extends ResourceManager<R, M>,
  T extends keyof R = keyof R
> =
  | [
      T,
      "=" | ">" | ">=" | "<" | "<=" | "<>" | "!=" | "is" | "is not" | "like",
      R[T]
    ]
  | [T, "in", R[T][]];

export type OrderByClause<
  R extends Resource<R, M>,
  M extends ResourceManager<R, M>
> = [key: keyof R, descending?: boolean];
