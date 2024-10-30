import EventEmitter, { type EventInterface } from '@rizzzi/eventemitter';

export enum LogLevel {
	Fatal,
	Error,
	Warn,
	Info,
	Debug
}

interface LoggerEventInterface extends EventInterface {
	log: [level: LogLevel, scope: string[], message: string, timestamp: number];
}

export class Logger {
	constructor(name: string) {
		this.#name = name;
		this.#subscribedLoggers = [];
		this.#events = new EventEmitter({
			requireErrorHandling: false
		});
	}

	readonly #name: string;
	readonly #subscribedLoggers: Logger[];
	readonly #events: EventEmitter<LoggerEventInterface, this>;

	get name() {
		return this.#name;
	}

	get on() {
		return this.#events.on.bind(this.#events);
	}

	get off() {
		return this.#events.off.bind(this.#events);
	}

	get once() {
		return this.#events.once.bind(this.#events);
	}

	subscribe(...loggers: Logger[]) {
		for (const logger of loggers) {
			logger.#subscribedLoggers.push(this);
		}
	}

	unsubscribe(...loggers: Logger[]) {
		for (const logger of [...loggers]) {
			const index = logger.#subscribedLoggers.indexOf(this);

			if (index == -1) {
				continue;
			}

			logger.#subscribedLoggers.splice(index, 1);
		}
	}

	#log(level: LogLevel, scope: string[], message: string, timestamp: number) {
		scope = [this.#name, ...scope];

		this.#events.emit('log', level, scope, message, timestamp);

		for (const subscriber of this.#subscribedLoggers) {
			subscriber.#log(level, scope, message, timestamp);
		}
	}

	log(level: LogLevel, message: string) {
		if (!(level in LogLevel)) {
			throw new Error(`Invalid loglevel: ${level}`);
		}

		this.#log(level, [], message, Date.now());
	}

	debug(message: string) {
		this.log(LogLevel.Debug, message);
	}

	info(message: string) {
		this.log(LogLevel.Info, message);
	}

	warn(message: string) {
		this.log(LogLevel.Warn, message);
	}

	error(message: string) {
		this.log(LogLevel.Error, message);
	}

	fatal(message: string) {
		this.log(LogLevel.Fatal, message);
	}
}
